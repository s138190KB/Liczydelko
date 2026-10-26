using Liczydelko.Dane;
using Liczydelko.Modele;

namespace Liczydelko.Serwisy;

public class OdznakaSerwis(KontekstBazy db, PostepSerwis postep)
{
    record Stan(
        int Quizy, bool BezbledowyQuiz, int Lekcje, int Seria, int Punkty, int Level,
        int PoprawneOdpowiedzi, int ZadaniaNaCzas, int ZadaniaZnajdzBlad);

    record Definicja(string Kod, string Nazwa, string Opis, string Ikona, Func<Stan, bool> Warunek);

    static readonly Definicja[] Definicje =
    [
        new("pierwszy-quiz", "Pierwszy krok", "Ukończ swój pierwszy quiz.", "★", stan => stan.Quizy >= 1),
        new("bezbledny-quiz", "Bezbłędny quiz", "Rozwiąż cały quiz bez jednego błędu.", "✓", stan => stan.BezbledowyQuiz),
        new("dziesiec-quizow", "Pracuś", "Ukończ 10 quizów.", "10", stan => stan.Quizy >= 10),
        new("pierwsza-lekcja", "Pierwsza lekcja", "Zalicz całą lekcję (quiz na każdym poziomie i wszystkie zadania).", "π", stan => stan.Lekcje >= 1),
        new("seria-3", "Trzy dni z rzędu", "Ucz się trzy dni z rzędu.", "3", stan => stan.Seria >= 3),
        new("seria-7", "Tydzień nauki", "Ucz się siedem dni z rzędu.", "7", stan => stan.Seria >= 7),
        new("punkty-100", "Setka punktów", "Zdobądź 100 punktów.", "100", stan => stan.Punkty >= 100),
        new("punkty-500", "Pięćset punktów", "Zdobądź 500 punktów.", "500", stan => stan.Punkty >= 500),
        new("mistrz-czasu", "Mistrz czasu", "Poprawnie rozwiąż 3 różne zadania na czas.", "⚡", stan => stan.ZadaniaNaCzas >= 3),
        new("lowca-bledow", "Łowca błędów", "Poprawnie rozwiąż 3 zadania typu „Znajdź błąd”.", "√", stan => stan.ZadaniaZnajdzBlad >= 3),
        new("piecdziesiat", "Pół setki", "Udziel 50 poprawnych odpowiedzi.", "50", stan => stan.PoprawneOdpowiedzi >= 50),
        new("level-2", "Level 2", "Osiągnij level 2.", "L2", stan => stan.Level >= 2),
        new("level-3", "Level 3", "Osiągnij level 3.", "L3", stan => stan.Level >= 3),
    ];

    public static void Synchronizuj(KontekstBazy db)
    {
        var istniejace = db.Odznaki.ToList();
        foreach (var definicja in Definicje)
        {
            var odznaka = istniejace.FirstOrDefault(istniejaca => istniejaca.Kod == definicja.Kod)
                ?? db.Odznaki.Add(new Odznaka { Kod = definicja.Kod }).Entity;
            odznaka.Nazwa = definicja.Nazwa;
            odznaka.Opis = definicja.Opis;
            odznaka.Ikona = definicja.Ikona;
        }
        var kody = Definicje.Select(definicja => definicja.Kod).ToHashSet();
        db.Odznaki.RemoveRange(istniejace.Where(odznaka => !kody.Contains(odznaka.Kod)));
        db.SaveChanges();
    }

    public List<Odznaka> Przyznaj(int uczenId)
    {
        var stan = PobierzStan(uczenId);
        var spelnione = Definicje.Where(definicja => definicja.Warunek(stan)).Select(definicja => definicja.Kod).ToHashSet();
        var zdobyte = db.OdznakiUzytkownikow.Where(zdobycie => zdobycie.UzytkownikId == uczenId).Select(zdobycie => zdobycie.OdznakaId).ToHashSet();

        var nowe = db.Odznaki.ToList().Where(odznaka => spelnione.Contains(odznaka.Kod) && !zdobyte.Contains(odznaka.Id)).ToList();
        if (nowe.Count == 0) return nowe;

        db.OdznakiUzytkownikow.AddRange(nowe.Select(odznaka =>
            new OdznakaUzytkownika { UzytkownikId = uczenId, OdznakaId = odznaka.Id, DataZdobycia = DateTime.UtcNow }));
        db.SaveChanges();
        return nowe;
    }

    public List<WierszOdznaki> Lista(int uczenId)
    {
        var zdobyte = db.OdznakiUzytkownikow.Where(zdobycie => zdobycie.UzytkownikId == uczenId)
            .ToDictionary(zdobycie => zdobycie.OdznakaId, zdobycie => zdobycie.DataZdobycia);
        return db.Odznaki.OrderBy(odznaka => odznaka.Id).ToList().Select(odznaka => new WierszOdznaki
        {
            Nazwa = odznaka.Nazwa,
            Opis = odznaka.Opis,
            Ikona = odznaka.Ikona,
            Zdobyta = zdobyte.ContainsKey(odznaka.Id),
            Data = zdobyte.TryGetValue(odznaka.Id, out var data) ? data : null
        }).ToList();
    }

    Stan PobierzStan(int uczenId)
    {
        var uczen = db.Uzytkownicy.Find(uczenId)!;
        var wyniki = db.Wyniki.Where(wynik => wynik.UzytkownikId == uczenId)
            .Select(wynik => new { wynik.LiczbaPunktow, wynik.Razem, wynik.Data }).ToList();
        var poprawneQuiz = db.Odpowiedzi.Where(odpowiedz => odpowiedz.UzytkownikId == uczenId && odpowiedz.CzyPoprawna)
            .Select(odpowiedz => odpowiedz.PytanieQuizuId).Distinct().Count();
        var poprawneZadania = db.WynikiCwiczen.Where(wynik => wynik.UzytkownikId == uczenId && wynik.CzyPoprawna)
            .Join(db.Zadania, wynik => wynik.ZadanieId, zadanie => zadanie.Id, (wynik, zadanie) => new { wynik.ZadanieId, zadanie.Rodzaj })
            .Distinct().ToList();

        return new Stan(
            Quizy: wyniki.Count,
            BezbledowyQuiz: wyniki.Any(wynik => wynik.Razem > 0 && wynik.LiczbaPunktow == wynik.Razem),
            Lekcje: postep.Zaliczone(uczenId).Count,
            Seria: PostepSerwis.AktualnaSeria(uczen.SeriaDni, wyniki.Count == 0 ? null : wyniki.Max(wynik => wynik.Data)),
            Punkty: uczen.Punkty,
            Level: uczen.Level,
            PoprawneOdpowiedzi: poprawneQuiz + poprawneZadania.Count,
            ZadaniaNaCzas: poprawneZadania.Count(zadanie => zadanie.Rodzaj == "Czas"),
            ZadaniaZnajdzBlad: poprawneZadania.Count(zadanie => zadanie.Rodzaj == "Blad"));
    }
}
