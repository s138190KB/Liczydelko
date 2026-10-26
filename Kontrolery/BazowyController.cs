using System.Security.Claims;
using Liczydelko.Modele;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Liczydelko.Kontrolery;

public abstract class BazowyController : Controller
{
    protected int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected bool CzyUczen => User.IsInRole(Role.Uczen);

    protected string? PierwszyBladWalidacji() =>
        ModelState.Values.SelectMany(wpis => wpis.Errors).Select(blad => blad.ErrorMessage).FirstOrDefault(komunikat => komunikat.Length > 0);

    protected Task Zaloguj(Uzytkownik uczen, bool maAwatar)
    {
        var dane = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, uczen.Id.ToString()),
            new(ClaimTypes.Name, uczen.Nick),
            new(ClaimTypes.Role, uczen.Rola)
        };
        if (maAwatar) dane.Add(new Claim("awatar", "1"));
        var tozsamosc = new ClaimsIdentity(dane, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(new ClaimsPrincipal(tozsamosc));
    }
}
