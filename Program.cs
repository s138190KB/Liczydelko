using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Liczydelko.Dane;
using Liczydelko.Serwisy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var korzen = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
    ? Directory.GetCurrentDirectory() : AppContext.BaseDirectory;

var b = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = korzen });

b.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddRazorOptions(o =>
    {
        o.ViewLocationFormats.Clear();
        o.ViewLocationFormats.Add("/Widoki/{1}/{0}.cshtml");
        o.ViewLocationFormats.Add("/Widoki/Shared/{0}.cshtml");
    });
b.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

b.Services.AddDbContext<KontekstBazy>(o => o
    .UseSqlite($"Data Source={Path.Combine(korzen, "liczydelko_baza")}")
    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

b.Services.AddScoped<QuizSerwis>();
b.Services.AddScoped<PostepSerwis>();
b.Services.AddScoped<AnalizaBledowSerwis>();
b.Services.AddScoped<RekomendacjaSerwis>();
b.Services.AddScoped<OdznakaSerwis>();
b.Services.AddScoped<KontoSerwis>();
b.Services.AddScoped<AwatarSerwis>();
b.Services.AddScoped<RankingSerwis>();
b.Services.AddDataProtection();
b.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Konto/Logowanie";
        o.AccessDeniedPath = "/Konto/BrakDostepu";
        o.Cookie.Name = "Liczydelko.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromDays(14);
        o.SlidingExpiration = true;
        o.Events.OnValidatePrincipal = async kontekst =>
        {
            var db = kontekst.HttpContext.RequestServices.GetRequiredService<KontekstBazy>();
            var istnieje = int.TryParse(kontekst.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var uczenId)
                && await db.Uzytkownicy.AnyAsync(uzytkownik => uzytkownik.Id == uczenId);
            if (!istnieje)
            {
                kontekst.RejectPrincipal();
                await kontekst.HttpContext.SignOutAsync();
            }
        };
    });

b.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.HttpContext.Response.WriteAsync("Zbyt wiele prób. Odczekaj chwilę i spróbuj ponownie.", ct);
    };
    o.AddPolicy("logowanie", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "nieznany",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = b.Build();
using (var s = app.Services.CreateScope())
    Inicjalizator.Wypelnij(s.ServiceProvider.GetRequiredService<KontekstBazy>(), Path.Combine(korzen, "Dane", "tresci.json"));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Blad");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/Blad", "?kod={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Nauka}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var adres = app.Urls.FirstOrDefault();
        if (adres == null) return;
        try
        {
            if (OperatingSystem.IsWindows()) Process.Start(new ProcessStartInfo(adres) { UseShellExecute = true });
            else if (OperatingSystem.IsMacOS()) Process.Start("open", adres);
            else Process.Start("xdg-open", adres);
        }
        catch { }
    });
}
app.Run();
