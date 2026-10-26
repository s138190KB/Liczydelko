using Liczydelko.Dane;
using Liczydelko.Modele;

namespace Liczydelko.Serwisy;

public class AnalizaBledowSerwis(KontekstBazy db, PostepSerwis postep)
{
    static readonly int[] PrzerwyDni = [0, 1, 3, 7, 14];
    const int MaxPudelko = 5;
    const int WpisowWRaporcie = 6;

    record InfoLekcji(int Id, string Tytul, string Kategoria);

    public static string Etykieta(string kategoria) => kategoria switch
    {
        "Rownania" => "Równania",
        "Ulamki" => "Ułamki",
        _ => kategoria
    };

    public void ZapiszOdpowiedzi(int uczenId, List<Ocena> oceny)
    {
        var teraz = DateTime.UtcNow;
        var idPytan = oceny.Select(ocena => ocena.Pytanie.Id).ToList();
        var powtorki = db.Powtorki
            .Where(powtorka => powtorka.UzytkownikId == uczenId && idPytan.Contains(powtorka.PytanieQuizuId))
            .ToDictionary(powtorka => powtorka.PytanieQuizuId);

        foreach (var ocena in oceny)
        {
            db.Odpowiedzi.Add(new OdpowiedzUzytkownika
            {
                UzytkownikId = uczenId,
                PytanieQuizuId = ocena.Pytanie.Id,
                WybranaOdp = ocena.Wybrana,
                CzyPoprawna = ocena.Poprawna,
                CzasSekundy = ocena.Czas,
                Data = teraz
            });
            powtorki.TryGetValue(ocena.Pytanie.Id, out var powtorka);

            if (!ocena.Poprawna)
            {
                powtorka ??= db.Powtorki.Add(new PowtorkaBledu { UzytkownikId = uczenId, PytanieQuizuId = ocena.Pytanie.Id }).Entity;
                powtorka.Pudelko = 1;
                powtorka.NastepnaPowtorka = teraz;
            }
            else if (powtorka != null && powtorka.NastepnaPowtorka <= teraz)
            {
                powtorka.Pudelko = Math.Max(powtorka.Pudelko, 1) + 1;
                if (powtorka.Pudelko > MaxPudelko) db.Powtorki.Remove(powtorka);
                else powtorka.NastepnaPowtorka = teraz.AddDays(PrzerwyDni[powtorka.Pudelko - 1]);
            }
        }
        db.SaveChanges();
    }

    public bool ZapiszCwiczenie(int uczenId, Zadanie zadanie, bool poprawna)
    {
        var juzPoprawne = db.WynikiCwiczen.Any(wynik => wynik.UzytkownikId == uczenId && wynik.ZadanieId == zadanie.Id && wynik.CzyPoprawna);
        db.WynikiCwiczen.Add(new WynikCwiczenia { UzytkownikId = uczenId, ZadanieId = zadanie.Id, CzyPoprawna = poprawna, Data = DateTime.UtcNow });
        db.SaveChanges();
        postep.UniewaznijPostep(uczenId);
        return poprawna && !juzPoprawne;
    }

    public HashSet<int> PoprawnieOdpowiedziane(int uczenId, List<int> pytania) =>
        db.Odpowiedzi.Where(odpowiedz => odpowiedz.UzytkownikId == uczenId && odpowiedz.CzyPoprawna && pytania.Contains(odpowiedz.PytanieQuizuId))
            .Select(odpowiedz => odpowiedz.PytanieQuizuId).Distinct().ToHashSet();

    IQueryable<PowtorkaBledu> PowtorkiDoZrobienia(int uczenId)
    {
        var teraz = DateTime.UtcNow;
        return db.Powtorki.Where(powtorka => powtorka.UzytkownikId == uczenId && powtorka.NastepnaPowtorka <= teraz);
    }

    IQueryable<PowtorkaBledu> PowtorkiZaplanowane(int uczenId)
    {
        var teraz = DateTime.UtcNow;
        return db.Powtorki.Where(powtorka => powtorka.UzytkownikId == uczenId && powtorka.NastepnaPowtorka > teraz);
    }

    public bool CzyWszystkieDoPowtorki(int uczenId, List<int> pytania) =>
        PowtorkiDoZrobienia(uczenId).Where(powtorka => pytania.Contains(powtorka.PytanieQuizuId))
            .Select(powtorka => powtorka.PytanieQuizuId).Distinct().Count() == pytania.Count;

    public List<PytanieQuizu> PobierzPowtorki(int uczenId) =>
        PowtorkiDoZrobienia(uczenId).OrderBy(powtorka => powtorka.NastepnaPowtorka).ThenBy(powtorka => powtorka.Pudelko)
            .Select(powtorka => powtorka.PytanieQuizu).Take(10).ToList();

    public DateTime? NajblizszaPowtorka(int uczenId) =>
        PowtorkiZaplanowane(uczenId).Min(powtorka => (DateTime?)powtorka.NastepnaPowtorka);

    public List<DzienWykresu> DaneWykresu(int uczenId, int dni = 14)
    {
        var pierwszyDzien = Czas.Dzisiaj.AddDays(-(dni - 1));
        var odUtc = Czas.PoczatekDniaUtc(pierwszyDzien);
        var quiz = db.Odpowiedzi.Where(odpowiedz => odpowiedz.UzytkownikId == uczenId && odpowiedz.Data >= odUtc)
            .Select(odpowiedz => new { odpowiedz.Data, odpowiedz.CzyPoprawna }).ToList();
        var zadania = db.WynikiCwiczen.Where(wynik => wynik.UzytkownikId == uczenId && wynik.Data >= odUtc)
            .Select(wynik => new { wynik.Data, wynik.CzyPoprawna }).ToList();
        var wpisyWgDni = quiz.Concat(zadania).ToLookup(wpis => Czas.Dzien(wpis.Data));

        return Enumerable.Range(0, dni).Select(numer =>
        {
            var dzien = pierwszyDzien.AddDays(numer);
            var razem = wpisyWgDni[dzien].Count();
            var poprawne = wpisyWgDni[dzien].Count(wpis => wpis.CzyPoprawna);
            return new DzienWykresu
            {
                Etykieta = dzien.ToString("dd.MM"),
                Razem = razem,
                Poprawne = poprawne,
                Procent = razem == 0 ? null : 100 * poprawne / razem
            };
        }).ToList();
    }

    public RaportDto Raport(int uczenId)
    {
        var lekcje = db.Lekcje
            .Select(lekcja => new InfoLekcji(lekcja.Id, lekcja.Tytul, lekcja.Dzial.Kategoria))
            .ToDictionary(lekcja => lekcja.Id);

        var wgKategorii = LiczbyWedlugLekcji(uczenId)
            .GroupBy(wiersz => lekcje[wiersz.LekcjaId].Kategoria)
            .ToDictionary(grupa => grupa.Key, grupa => (
                Poprawne: grupa.Where(wiersz => wiersz.Poprawna).Sum(wiersz => wiersz.Liczba),
                Bledne: grupa.Where(wiersz => !wiersz.Poprawna).Sum(wiersz => wiersz.Liczba)));
        var poprawneRazem = wgKategorii.Values.Sum(liczby => liczby.Poprawne);
        var bledneRazem = wgKategorii.Values.Sum(liczby => liczby.Bledne);

        var zaliczone = postep.Zaliczone(uczenId);
        var kategorie = lekcje.Values.GroupBy(lekcja => lekcja.Kategoria).Select(grupa =>
        {
            var (poprawne, bledne) = wgKategorii.GetValueOrDefault(grupa.Key);
            return new WierszKategorii
            {
                Klucz = grupa.Key,
                Nazwa = Etykieta(grupa.Key),
                Poprawne = poprawne,
                Bledne = bledne,
                LekcjiRazem = grupa.Count(),
                LekcjiZaliczonych = grupa.Count(lekcja => zaliczone.Contains(lekcja.Id))
            };
        }).OrderBy(wiersz => wiersz.Nazwa).ToList();

        var raport = new RaportDto
        {
            Poprawne = poprawneRazem,
            Bledne = bledneRazem,
            DoPowtorki = PowtorkiDoZrobienia(uczenId).Count(),
            Zaplanowane = PowtorkiZaplanowane(uczenId).Count(),
            WszystkieLekcje = lekcje.Count,
            UkonczoneLekcje = zaliczone.Count,
            Kategorie = kategorie,
            BledneWpisy = OstatnieWpisy(uczenId, poprawne: false, lekcje),
            PoprawneWpisy = OstatnieWpisy(uczenId, poprawne: true, lekcje),
            Quizy = db.Wyniki.Where(wynik => wynik.UzytkownikId == uczenId).OrderByDescending(wynik => wynik.Data).Take(8)
                .Select(wynik => new WynikQuizuWiersz { Lekcja = wynik.Lekcja.Tytul, Trudnosc = wynik.Trudnosc, Punkty = wynik.LiczbaPunktow, Razem = wynik.Razem, Data = wynik.Data })
                .ToList()
        };

        var maWyniki = poprawneRazem + bledneRazem > 0;
        if (!maWyniki)
            raport.Sugestie.Add(new("Nie masz jeszcze żadnych wyników. Zacznij od pierwszej lekcji, zadań i quizu na łatwym poziomie.", null, false));
        foreach (var kategoria in kategorie.Where(wiersz => wiersz.Poprawne + wiersz.Bledne >= 3 && wiersz.Poprawnosc < 70).OrderBy(wiersz => wiersz.Poprawnosc))
        {
            var pierwszaLekcja = lekcje.Values.FirstOrDefault(info => info.Kategoria == kategoria.Klucz);
            raport.Sugestie.Add(new($"Poćwicz dział: {kategoria.Nazwa}. Poprawnych odpowiedzi: {kategoria.Poprawnosc}%.", pierwszaLekcja?.Id, false));
        }
        if (raport.DoPowtorki > 0)
            raport.Sugestie.Add(new($"Masz {raport.DoPowtorki} błędnych pytań do powtórki. Rozwiąż je ponownie.", null, true));
        if (maWyniki && raport.Sugestie.Count == 0)
            raport.Sugestie.Add(new("Wyniki są dobre. Spróbuj quizu na wyższym poziomie trudności albo zadań na czas.", null, false));
        return raport;
    }

    List<(int LekcjaId, bool Poprawna, int Liczba)> LiczbyWedlugLekcji(int uczenId)
    {
        var quiz = db.Odpowiedzi.Where(odpowiedz => odpowiedz.UzytkownikId == uczenId)
            .Join(db.PytaniaQuizu, odpowiedz => odpowiedz.PytanieQuizuId, pytanie => pytanie.Id,
                (odpowiedz, pytanie) => new { pytanie.LekcjaId, odpowiedz.CzyPoprawna })
            .GroupBy(wpis => new { wpis.LekcjaId, wpis.CzyPoprawna })
            .Select(grupa => new { grupa.Key.LekcjaId, grupa.Key.CzyPoprawna, Liczba = grupa.Count() }).ToList();
        var zadania = db.WynikiCwiczen.Where(wynik => wynik.UzytkownikId == uczenId)
            .Join(db.Zadania, wynik => wynik.ZadanieId, zadanie => zadanie.Id,
                (wynik, zadanie) => new { zadanie.LekcjaId, wynik.CzyPoprawna })
            .GroupBy(wpis => new { wpis.LekcjaId, wpis.CzyPoprawna })
            .Select(grupa => new { grupa.Key.LekcjaId, grupa.Key.CzyPoprawna, Liczba = grupa.Count() }).ToList();
        return quiz.Concat(zadania).Select(wiersz => (wiersz.LekcjaId, wiersz.CzyPoprawna, wiersz.Liczba)).ToList();
    }

    List<WpisRaportu> OstatnieWpisy(int uczenId, bool poprawne, Dictionary<int, InfoLekcji> lekcje) =>
        [.. OstatnieWpisyQuizu(uczenId, poprawne, lekcje), .. OstatnieWpisyZadan(uczenId, poprawne, lekcje)];

    List<WpisRaportu> OstatnieWpisyQuizu(int uczenId, bool poprawne, Dictionary<int, InfoLekcji> lekcje) =>
        db.Odpowiedzi.Where(odpowiedz => odpowiedz.UzytkownikId == uczenId && odpowiedz.CzyPoprawna == poprawne)
            .Join(db.PytaniaQuizu, odpowiedz => odpowiedz.PytanieQuizuId, pytanie => pytanie.Id,
                (odpowiedz, pytanie) => new { odpowiedz.Id, pytanie.LekcjaId, pytanie.Tresc, pytanie.OdpA, pytanie.OdpB, pytanie.OdpC, pytanie.OdpD, pytanie.Poprawna })
            .OrderByDescending(wpis => wpis.Id).Take(WpisowWRaporcie).ToList()
            .Select(wpis => new WpisRaportu
            {
                Zrodlo = "Quiz",
                Tresc = wpis.Tresc,
                Odpowiedz = Tekst(wpis.Poprawna, wpis.OdpA, wpis.OdpB, wpis.OdpC, wpis.OdpD),
                LekcjaId = wpis.LekcjaId,
                Lekcja = lekcje[wpis.LekcjaId].Tytul,
                Kategoria = lekcje[wpis.LekcjaId].Kategoria,
                CzyPoprawna = poprawne
            }).ToList();

    List<WpisRaportu> OstatnieWpisyZadan(int uczenId, bool poprawne, Dictionary<int, InfoLekcji> lekcje) =>
        db.WynikiCwiczen.Where(wynik => wynik.UzytkownikId == uczenId && wynik.CzyPoprawna == poprawne)
            .Join(db.Zadania, wynik => wynik.ZadanieId, zadanie => zadanie.Id,
                (wynik, zadanie) => new { wynik.Id, zadanie.LekcjaId, zadanie.Tresc, zadanie.Odpowiedz })
            .OrderByDescending(wpis => wpis.Id).Take(WpisowWRaporcie).ToList()
            .Select(wpis => new WpisRaportu
            {
                Zrodlo = "Zadanie",
                Tresc = wpis.Tresc.Replace("[]", "___"),
                Odpowiedz = wpis.Odpowiedz.Replace("|", " lub ").Replace(";", ", "),
                LekcjaId = wpis.LekcjaId,
                Lekcja = lekcje[wpis.LekcjaId].Tytul,
                Kategoria = lekcje[wpis.LekcjaId].Kategoria,
                CzyPoprawna = poprawne
            }).ToList();

    static string Tekst(string litery, string odpA, string odpB, string odpC, string odpD) =>
        string.Join(", ", litery.Select(litera => litera switch { 'A' => odpA, 'B' => odpB, 'C' => odpC, _ => odpD }));
}
