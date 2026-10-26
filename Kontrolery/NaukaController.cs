using Liczydelko.Dane;
using Liczydelko.Modele;
using Liczydelko.Serwisy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Liczydelko.Kontrolery;

public class NaukaController(
    KontekstBazy db, QuizSerwis quiz, PostepSerwis postep, AnalizaBledowSerwis analiza,
    OdznakaSerwis odznaki, RekomendacjaSerwis rekomendacje, RankingSerwis ranking) : BazowyController
{
    record Nagrody(bool Awans, string? Level, List<string> Odznaki);

    public IActionResult Index(int? poziom)
    {
        var wszystkie = db.PoziomyEdukacyjne
            .Include(wiersz => wiersz.Dzialy).ThenInclude(dzial => dzial.Lekcje)
            .OrderBy(wiersz => wiersz.Kolejnosc).ToList();
        ViewBag.Poziom = poziom;
        ViewBag.Wszystkie = wszystkie;
        ViewBag.Zaliczone = CzyUczen ? postep.Zaliczone(Uid) : new HashSet<int>();
        return View(poziom == null ? wszystkie : wszystkie.Where(wiersz => wiersz.Kolejnosc == poziom).ToList());
    }

    public IActionResult Lekcja(int id)
    {
        var lekcja = db.Lekcje.Include(wiersz => wiersz.Przyklady).Include(wiersz => wiersz.Dzial).FirstOrDefault(wiersz => wiersz.Id == id);
        if (lekcja == null) return NotFound();
        var (trudnosc, powod) = CzyUczen ? rekomendacje.PolecanaTrudnosc(Uid, id) : (1, (string?)null);
        return View(new ModelLekcji
        {
            Lekcja = lekcja,
            Postep = CzyUczen ? postep.PobierzPostepLekcji(Uid, id) : null,
            Pytania = CzyUczen ? quiz.PobierzPytania(id) : new(),
            Zadania = db.Zadania.Where(zadanie => zadanie.LekcjaId == id).OrderBy(zadanie => zadanie.Id).ToList(),
            Polecana = trudnosc,
            PowodPolecenia = powod
        });
    }

    [Authorize(Roles = Role.Uczen)]
    public IActionResult Powtorka() => View(analiza.Raport(Uid));

    [Authorize(Roles = Role.Uczen)]
    public IActionResult PowtorzBledy()
    {
        ViewBag.Najblizsza = analiza.NajblizszaPowtorka(Uid);
        return View(new ModelLekcji
        {
            Lekcja = new Lekcja { Id = 0, Tytul = "Powtórka błędnych pytań" },
            Pytania = analiza.PobierzPowtorki(Uid)
        });
    }

    public IActionResult Ranking() => View(ranking.Zbuduj(CzyUczen ? Uid : null));

    [Authorize(Roles = Role.Uczen)]
    public IActionResult DaneWykresu() => Json(analiza.DaneWykresu(Uid));

    [Authorize(Roles = Role.Uczen), HttpPost]
    public IActionResult Zakoncz([FromBody] ZakonczDto dane)
    {
        if (dane == null || !ModelState.IsValid || dane.Odpowiedzi.Any(odpowiedz => odpowiedz == null)) return BadRequest();
        var trudnosc = Math.Clamp(dane.Trudnosc, 1, 3);
        var wyslanePytania = dane.Odpowiedzi.Select(odpowiedz => odpowiedz.PytanieId).ToList();
        if (wyslanePytania.Distinct().Count() != wyslanePytania.Count) return BadRequest();

        var wLekcji = dane.LekcjaId > 0;
        if (wLekcji)
        {
            if (!db.Lekcje.Any(lekcja => lekcja.Id == dane.LekcjaId)) return NotFound();
            if (!quiz.IdPytan(dane.LekcjaId, trudnosc).SetEquals(wyslanePytania)) return BadRequest();
        }
        else if (!analiza.CzyWszystkieDoPowtorki(Uid, wyslanePytania)) return BadRequest();

        var juzPoprawne = analiza.PoprawnieOdpowiedziane(Uid, wyslanePytania);
        var oceny = quiz.Sprawdz(dane.Odpowiedzi);
        var nowePoprawne = oceny.Count(ocena => ocena.Poprawna && !juzPoprawne.Contains(ocena.Pytanie.Id));

        analiza.ZapiszOdpowiedzi(Uid, oceny);
        var punkty = oceny.Count(ocena => ocena.Poprawna);
        var zaliczona = wLekcji && postep.ZapiszWynik(Uid, dane.LekcjaId, punkty, oceny.Count, trudnosc, nowePoprawne);
        var nagrody = PrzyznajNagrody();
        return Json(new
        {
            punkty,
            razem = oceny.Count,
            nowePunkty = wLekcji ? nowePoprawne * 10 * trudnosc : 0,
            zaliczona,
            awans = nagrody.Awans,
            level = nagrody.Level,
            odznaki = nagrody.Odznaki
        });
    }

    [HttpPost]
    public IActionResult SprawdzZadanie([FromBody] SprawdzDto dane)
    {
        if (dane == null || !ModelState.IsValid) return BadRequest();
        var (zadanie, poprawna) = quiz.SprawdzZadanie(dane.ZadanieId, dane.Odpowiedz);
        if (zadanie == null) return NotFound();

        var punkty = 0;
        if (CzyUczen && analiza.ZapiszCwiczenie(Uid, zadanie, poprawna))
        {
            postep.DodajPunkty(Uid, 5);
            punkty = 5;
        }
        var nagrody = CzyUczen && poprawna ? PrzyznajNagrody() : new Nagrody(false, null, []);
        return Json(new
        {
            awans = nagrody.Awans,
            level = nagrody.Level,
            poprawna,
            wzorcowa = zadanie.Odpowiedz.Replace("|", " lub ").Replace(";", ", "),
            wyjasnienie = zadanie.Wyjasnienie,
            punkty,
            odznaki = nagrody.Odznaki
        });
    }

    Nagrody PrzyznajNagrody()
    {
        var level = postep.PrzeliczLevel(Uid);
        return new Nagrody(level.Nowy > level.Stary, level.Nazwa, odznaki.Przyznaj(Uid).Select(odznaka => odznaka.Nazwa).ToList());
    }
}
