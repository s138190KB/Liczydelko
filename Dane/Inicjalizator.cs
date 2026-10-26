using System.Text.Json;
using Liczydelko.Modele;
using Liczydelko.Serwisy;
using Microsoft.EntityFrameworkCore;

namespace Liczydelko.Dane;

public class PlikTresci
{
    public List<DefinicjaLevelu> Levele { get; set; } = new();
    public List<PoziomEdukacyjny> Poziomy { get; set; } = new();
}

public static class Inicjalizator
{
    static readonly JsonSerializerOptions OpcjeJson = new() { PropertyNameCaseInsensitive = true };

    public static void Wypelnij(KontekstBazy db, string sciezkaTresci)
    {
        db.Database.Migrate();
        OdznakaSerwis.Synchronizuj(db);

        var plik = JsonSerializer.Deserialize<PlikTresci>(File.ReadAllText(sciezkaTresci), OpcjeJson)!;
        SynchronizujLevele(db, plik.Levele);
        SynchronizujTresci(db, plik.Poziomy);

        if (db.SaveChanges() > 0 || db.Uzytkownicy.Any(uczen => uczen.Level < Uzytkownik.Level1))
        {
            var postep = new PostepSerwis(db);
            foreach (var uczenId in db.Uzytkownicy.Select(uczen => uczen.Id).ToList())
                postep.PrzeliczLevel(uczenId);
        }
    }

    static void SynchronizujLevele(KontekstBazy db, List<DefinicjaLevelu> zPliku)
    {
        var wBazie = db.DefinicjeLeveli.ToList();
        var numery = zPliku.Select(level => level.Numer).ToHashSet();
        db.DefinicjeLeveli.RemoveRange(wBazie.Where(level => !numery.Contains(level.Numer)));

        foreach (var nowy in zPliku)
        {
            var istniejacy = wBazie.FirstOrDefault(level => level.Numer == nowy.Numer);
            if (istniejacy == null) db.DefinicjeLeveli.Add(nowy);
            else Kopiuj(db, istniejacy, nowy);
        }
    }

    static void SynchronizujTresci(KontekstBazy db, List<PoziomEdukacyjny> zPliku)
    {
        db.PoziomyEdukacyjne
            .Include(poziom => poziom.Dzialy).ThenInclude(dzial => dzial.Lekcje).ThenInclude(lekcja => lekcja.Przyklady)
            .Include(poziom => poziom.Dzialy).ThenInclude(dzial => dzial.Lekcje).ThenInclude(lekcja => lekcja.Pytania)
            .Include(poziom => poziom.Dzialy).ThenInclude(dzial => dzial.Lekcje).ThenInclude(lekcja => lekcja.Zadania)
            .AsSplitQuery().Load();

        Polacz(db, db.PoziomyEdukacyjne.Local, zPliku, (stary, nowy) => stary.Kolejnosc == nowy.Kolejnosc, (poziom, plikPoziom) =>
            Polacz(db, poziom.Dzialy, plikPoziom.Dzialy, (stary, nowy) => stary.Nazwa == nowy.Nazwa, (dzial, plikDzial) =>
                Polacz(db, dzial.Lekcje, plikDzial.Lekcje, (stary, nowy) => stary.Tytul == nowy.Tytul, (lekcja, plikLekcja) =>
                {
                    Polacz(db, lekcja.Przyklady, plikLekcja.Przyklady, (stary, nowy) => stary.Tresc == nowy.Tresc);
                    Polacz(db, lekcja.Pytania, plikLekcja.Pytania, (stary, nowy) => stary.Tresc == nowy.Tresc);
                    Polacz(db, lekcja.Zadania, plikLekcja.Zadania, (stary, nowy) => stary.Tresc == nowy.Tresc);
                })));
    }

    static void Polacz<T>(KontekstBazy db, ICollection<T> wBazie, IEnumerable<T> zPliku, Func<T, T, bool> takiSam, Action<T, T>? potomki = null)
        where T : class, IMaKod
    {
        foreach (var nowy in zPliku.ToList())
        {
            if (string.IsNullOrEmpty(nowy.Kod)) throw new InvalidOperationException("Rekord w tresci.json nie ma pola \"kod\".");
            var istniejacy = wBazie.FirstOrDefault(stary => stary.Kod == nowy.Kod)
                ?? wBazie.FirstOrDefault(stary => stary.Kod == null && takiSam(stary, nowy));
            if (istniejacy == null) { wBazie.Add(nowy); continue; }

            Kopiuj(db, istniejacy, nowy);
            potomki?.Invoke(istniejacy, nowy);
        }
    }

    static void Kopiuj<T>(KontekstBazy db, T cel, T zrodlo) where T : class
    {
        foreach (var pole in db.Entry(cel).Properties.Where(p => !p.Metadata.IsPrimaryKey() && !p.Metadata.IsForeignKey()))
            pole.CurrentValue = pole.Metadata.PropertyInfo!.GetValue(zrodlo);
    }
}
