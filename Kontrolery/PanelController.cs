using System.Security.Cryptography;
using Liczydelko.Dane;
using Liczydelko.Modele;
using Liczydelko.Serwisy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Liczydelko.Kontrolery;

[Authorize, AutoValidateAntiforgeryToken]
public class PanelController(
    KontekstBazy db, RekomendacjaSerwis rekomendacje, PostepSerwis postep, OdznakaSerwis odznaki,
    KontoSerwis konto, AwatarSerwis awatary) : BazowyController
{
    public IActionResult Index(string? karta)
    {
        var uczen = db.Uzytkownicy.Find(Uid)!;
        var teraz = DateTime.UtcNow;
        var seria = postep.OstatnieQuizy([Uid]).TryGetValue(Uid, out var ostatniQuiz)
            ? PostepSerwis.AktualnaSeria(uczen.SeriaDni, ostatniQuiz)
            : 0;

        ViewBag.Karta = karta == "konto" ? "konto" : "przeglad";
        return View(new ModelPanelu
        {
            Uzytkownik = uczen,
            MaAwatar = awatary.MaAwatar(Uid),
            SeriaDni = seria,
            Aktualny = db.DefinicjeLeveli.FirstOrDefault(definicja => definicja.Numer == uczen.Level)
                ?? new DefinicjaLevelu { Numer = Uzytkownik.Level1, Nazwa = "Początkujący", Opis = "" },
            Leveli = postep.StanLeveli(Uid),
            Rekomendacja = rekomendacje.Zalec(uczen),
            Wszystkie = db.Lekcje.Count(),
            Zaliczone = postep.Zaliczone(Uid).Count,
            Powtorki = db.Powtorki.Count(powtorka => powtorka.UzytkownikId == Uid && powtorka.NastepnaPowtorka <= teraz),
            Odznaki = odznaki.Lista(Uid)
        });
    }

    public async Task<IActionResult> Awatar()
    {
        var awatar = await awatary.PobierzAsync(Uid);
        if (awatar == null) return NotFound();
        var etag = new EntityTagHeaderValue($"\"{Convert.ToHexString(SHA256.HashData(awatar.Dane))}\"");
        Response.Headers.CacheControl = "private, max-age=0, must-revalidate";
        return File(awatar.Dane, awatar.Typ, null, etag);
    }

    [HttpPost]
    public async Task<IActionResult> ZmienHaslo(ZmianaHaslaModel model)
    {
        if (!ModelState.IsValid) return Wroc(blad: PierwszyBladWalidacji());
        return await konto.ZmienHasloAsync(Uid, model.Stare, model.Nowe)
            ? Wroc(info: "Hasło zostało zmienione.")
            : Wroc(blad: "Aktualne hasło jest nieprawidłowe.");
    }

    [HttpPost]
    public async Task<IActionResult> UsunKonto(UsuniecieKontaModel model)
    {
        if (!ModelState.IsValid) return Wroc(blad: PierwszyBladWalidacji());
        if (!await konto.UsunKontoAsync(Uid, model.Haslo))
            return Wroc(blad: "Hasło jest nieprawidłowe. Konto nie zostało usunięte.");

        await HttpContext.SignOutAsync();
        TempData["Info"] = "Konto i wszystkie jego dane zostały usunięte.";
        return RedirectToAction("Logowanie", "Konto");
    }

    [HttpPost]
    public async Task<IActionResult> UstawAwatar(IFormFile? plik)
    {
        var blad = await awatary.ZapiszAsync(Uid, plik);
        if (blad != null) return Wroc(blad: blad);
        await OdswiezLogowanie(maAwatar: true);
        return Wroc(info: "Awatar został zapisany.");
    }

    [HttpPost]
    public async Task<IActionResult> UsunAwatar()
    {
        await awatary.UsunAsync(Uid);
        await OdswiezLogowanie(maAwatar: false);
        return Wroc(info: "Awatar został usunięty.");
    }

    IActionResult Wroc(string? info = null, string? blad = null)
    {
        TempData["Info"] = info;
        TempData["Blad"] = blad;
        return RedirectToAction("Index", new { karta = "konto" });
    }

    async Task OdswiezLogowanie(bool maAwatar) =>
        await Zaloguj((await db.Uzytkownicy.FindAsync(Uid))!, maAwatar);
}
