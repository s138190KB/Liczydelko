using Liczydelko.Dane;
using Liczydelko.Modele;

namespace Liczydelko.Serwisy;

public class PostepSerwis(KontekstBazy db)
{
    readonly Dictionary<int, HashSet<int>> zaliczoneWgUcznia = new();

    static bool Zdane(int punkty, int razem, int wymaganyWynik) => razem > 0 && punkty * 100 >= razem * wymaganyWynik;

    public static int AktualnaSeria(int zapisana, DateTime? ostatniQuizUtc) =>
        ostatniQuizUtc is { } ostatni && Czas.Dzisiaj.DayNumber - Czas.Dzien(ostatni).DayNumber <= 1 ? zapisana : 0;

    public Dictionary<int, DateTime> OstatnieQuizy(List<int> uczniowie) =>
        db.Wyniki.Where(wynik => uczniowie.Contains(wynik.UzytkownikId))
            .GroupBy(wynik => wynik.UzytkownikId)
            .Select(grupa => new { UczenId = grupa.Key, Data = grupa.Max(wynik => wynik.Data) })
            .ToDictionary(wiersz => wiersz.UczenId, wiersz => wiersz.Data);

    public void UniewaznijPostep(int uczenId) => zaliczoneWgUcznia.Remove(uczenId);

    public HashSet<int> Zaliczone(int uczenId)
    {
        if (zaliczoneWgUcznia.TryGetValue(uczenId, out var zapisane)) return zapisane;

        var wymaganyWynikLekcji = db.Lekcje.Select(lekcja => new { lekcja.Id, lekcja.WymaganyWynik })
            .ToDictionary(lekcja => lekcja.Id, lekcja => lekcja.WymaganyWynik);
        var trudnosciLekcji = db.PytaniaQuizu.Select(pytanie => new { pytanie.LekcjaId, pytanie.Trudnosc }).Distinct().ToList()
            .ToLookup(wiersz => wiersz.LekcjaId, wiersz => wiersz.Trudnosc);
        var liczbaZadanWLekcji = db.Zadania.GroupBy(zadanie => zadanie.LekcjaId)
            .Select(grupa => new { LekcjaId = grupa.Key, Liczba = grupa.Count() })
            .ToDictionary(wiersz => wiersz.LekcjaId, wiersz => wiersz.Liczba);

        var zdaneQuizy = db.Wyniki.Where(wynik => wynik.UzytkownikId == uczenId)
            .Select(wynik => new { wynik.LekcjaId, wynik.Trudnosc, wynik.LiczbaPunktow, wynik.Razem }).ToList()
            .Where(wynik => wymaganyWynikLekcji.TryGetValue(wynik.LekcjaId, out var wymagany) && Zdane(wynik.LiczbaPunktow, wynik.Razem, wymagany))
            .Select(wynik => (wynik.LekcjaId, wynik.Trudnosc))
            .ToHashSet();

        var poprawneZadaniaWLekcji = db.WynikiCwiczen.Where(wynik => wynik.UzytkownikId == uczenId && wynik.CzyPoprawna)
            .Join(db.Zadania, wynik => wynik.ZadanieId, zadanie => zadanie.Id, (wynik, zadanie) => new { zadanie.LekcjaId, wynik.ZadanieId })
            .Distinct().ToList()
            .GroupBy(wiersz => wiersz.LekcjaId)
            .ToDictionary(grupa => grupa.Key, grupa => grupa.Count());

        var zaliczoneLekcje = wymaganyWynikLekcji.Keys.Where(lekcjaId =>
        {
            var trudnosci = trudnosciLekcji[lekcjaId];
            var zadaniaRazem = liczbaZadanWLekcji.GetValueOrDefault(lekcjaId);
            return (trudnosci.Any() || zadaniaRazem > 0)
                && trudnosci.All(trudnosc => zdaneQuizy.Contains((lekcjaId, trudnosc)))
                && poprawneZadaniaWLekcji.GetValueOrDefault(lekcjaId) >= zadaniaRazem;
        }).ToHashSet();

        zaliczoneWgUcznia[uczenId] = zaliczoneLekcje;
        return zaliczoneLekcje;
    }

    public PostepLekcji PobierzPostepLekcji(int uczenId, int lekcjaId)
    {
        var wymagany = db.Lekcje.Where(lekcja => lekcja.Id == lekcjaId).Select(lekcja => lekcja.WymaganyWynik).FirstOrDefault();
        var wymaganeTrudnosci = db.PytaniaQuizu.Where(pytanie => pytanie.LekcjaId == lekcjaId)
            .Select(pytanie => pytanie.Trudnosc).Distinct().OrderBy(trudnosc => trudnosc).ToList();
        var zaliczoneTrudnosci = db.Wyniki.Where(wynik => wynik.UzytkownikId == uczenId && wynik.LekcjaId == lekcjaId)
            .Select(wynik => new { wynik.Trudnosc, wynik.LiczbaPunktow, wynik.Razem }).ToList()
            .Where(wynik => Zdane(wynik.LiczbaPunktow, wynik.Razem, wymagany)).Select(wynik => wynik.Trudnosc).ToHashSet();
        var idZadan = db.Zadania.Where(zadanie => zadanie.LekcjaId == lekcjaId).Select(zadanie => zadanie.Id).ToList();
        var poprawneZadania = db.WynikiCwiczen
            .Where(wynik => wynik.UzytkownikId == uczenId && wynik.CzyPoprawna && idZadan.Contains(wynik.ZadanieId))
            .Select(wynik => wynik.ZadanieId).Distinct().Count();
        return new PostepLekcji(zaliczoneTrudnosci, wymaganeTrudnosci, poprawneZadania, idZadan.Count);
    }

    public List<StanLevelu> StanLeveli(int uczenId)
    {
        var zaliczone = Zaliczone(uczenId);
        var lekcje = db.Lekcje.Select(lekcja => new { lekcja.Id, Etap = lekcja.Dzial.PoziomEdukacyjny.Kolejnosc }).ToList();
        return db.DefinicjeLeveli.Where(definicja => definicja.Numer > Uzytkownik.Level1).OrderBy(definicja => definicja.Numer).ToList()
            .Select(definicja =>
            {
                var wymagane = lekcje.Where(lekcja => lekcja.Etap <= definicja.WymaganyEtap).ToList();
                return new StanLevelu(definicja.Numer, definicja.Nazwa, definicja.Opis, wymagane.Count,
                    wymagane.Count(lekcja => zaliczone.Contains(lekcja.Id)));
            }).ToList();
    }

    public (int Stary, int Nowy, string? Nazwa) PrzeliczLevel(int uczenId)
    {
        var uczen = db.Uzytkownicy.Find(uczenId)!;
        var level = StanLeveli(uczenId)
            .TakeWhile(stan => stan.Wymagane > 0 && stan.Zrobione == stan.Wymagane)
            .Select(stan => stan.Numer)
            .LastOrDefault(Uzytkownik.Level1);

        var stary = Math.Max(uczen.Level, Uzytkownik.Level1);
        if (uczen.Level != level)
        {
            uczen.Level = level;
            db.SaveChanges();
        }
        var nazwa = db.DefinicjeLeveli.Where(definicja => definicja.Numer == level).Select(definicja => definicja.Nazwa).FirstOrDefault();
        return (stary, level, nazwa);
    }

    public void DodajPunkty(int uczenId, int punkty)
    {
        db.Uzytkownicy.Find(uczenId)!.Punkty += punkty;
        db.SaveChanges();
    }

    public bool ZapiszWynik(int uczenId, int lekcjaId, int punkty, int razem, int trudnosc, int nowePoprawne)
    {
        var uczen = db.Uzytkownicy.Find(uczenId)!;
        var lekcja = db.Lekcje.Find(lekcjaId)!;
        var ostatniQuiz = db.Wyniki.Where(wynik => wynik.UzytkownikId == uczenId).OrderByDescending(wynik => wynik.Data)
            .Select(wynik => (DateTime?)wynik.Data).FirstOrDefault();

        if (ostatniQuiz is not { } ostatni || Czas.Dzien(ostatni) != Czas.Dzisiaj)
            uczen.SeriaDni = AktualnaSeria(uczen.SeriaDni, ostatniQuiz) + 1;

        db.Wyniki.Add(new Wynik
        {
            UzytkownikId = uczenId,
            LekcjaId = lekcjaId,
            LiczbaPunktow = punkty,
            Razem = razem,
            Trudnosc = trudnosc,
            Data = DateTime.UtcNow
        });
        uczen.Punkty += nowePoprawne * 10 * trudnosc;
        db.SaveChanges();
        UniewaznijPostep(uczenId);
        return Zdane(punkty, razem, lekcja.WymaganyWynik);
    }
}
