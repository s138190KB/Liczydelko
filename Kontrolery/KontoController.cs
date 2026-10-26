using Liczydelko.Modele;
using Liczydelko.Serwisy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Liczydelko.Kontrolery;

public class KontoController(KontoSerwis konto, AwatarSerwis awatary) : BazowyController
{
    const string BladLogowania =
        "Nie udało się zalogować. Sprawdź e-mail i hasło albo spróbuj ponownie za kilka minut " +
        "(po kilku nieudanych próbach logowanie do konta jest tymczasowo wstrzymane).";

    public IActionResult Logowanie() => View();

    [HttpPost, EnableRateLimiting("logowanie")]
    public async Task<IActionResult> Logowanie(LogowanieModel model)
    {
        var uczen = ModelState.IsValid ? await konto.ZalogujAsync(model.Email, model.Haslo) : null;
        return uczen == null
            ? PokazBlad(BladLogowania)
            : await ZalogujIPrzejdzDoPanelu(uczen, await awatary.MaAwatarAsync(uczen.Id));
    }

    public IActionResult Rejestracja() => View();

    [HttpPost, EnableRateLimiting("logowanie")]
    public async Task<IActionResult> Rejestracja(RejestracjaModel model)
    {
        if (!ModelState.IsValid)
            return PokazBlad(PierwszyBladWalidacji() ?? "Podaj e-mail, nick i hasło (min. 6 znaków).");

        var uczen = await konto.ZarejestrujAsync(model.Email, model.Haslo, model.Nick);
        return uczen == null
            ? PokazBlad("Ten e-mail jest już zajęty.")
            : await ZalogujIPrzejdzDoPanelu(uczen, maAwatar: false);
    }

    [HttpPost]
    public async Task<IActionResult> Wyloguj()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Logowanie");
    }

    public IActionResult BrakDostepu() => View();

    IActionResult PokazBlad(string komunikat)
    {
        ViewBag.Blad = komunikat;
        return View();
    }

    async Task<IActionResult> ZalogujIPrzejdzDoPanelu(Uzytkownik uczen, bool maAwatar)
    {
        await Zaloguj(uczen, maAwatar);
        return RedirectToAction("Index", "Panel");
    }
}
