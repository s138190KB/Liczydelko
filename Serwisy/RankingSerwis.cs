using Liczydelko.Dane;
using Liczydelko.Modele;

namespace Liczydelko.Serwisy;

public class RankingSerwis(KontekstBazy db, PostepSerwis postep)
{
    const int MiejscWRankingu = 20;

    record Pozycja(int Id, string Nick, int Punkty, int Level, int SeriaDni);

    IQueryable<Uzytkownik> Uczniowie => db.Uzytkownicy.Where(uczen => uczen.Rola == Role.Uczen);

    public ModelRankingu Zbuduj(int? zalogowanyUczenId)
    {
        var nazwyLeveli = db.DefinicjeLeveli.ToDictionary(definicja => definicja.Numer, definicja => definicja.Nazwa);

        var najlepsi = Uczniowie
            .OrderByDescending(uczen => uczen.Punkty).ThenBy(uczen => uczen.DataRejestracji).ThenBy(uczen => uczen.Id)
            .Take(MiejscWRankingu)
            .Select(uczen => new Pozycja(uczen.Id, uczen.Nick, uczen.Punkty, uczen.Level, uczen.SeriaDni)).ToList();

        var potrzebni = najlepsi.Select(pozycja => pozycja.Id).ToList();
        if (zalogowanyUczenId is { } uczenId) potrzebni.Add(uczenId);
        var ostatnieQuizy = postep.OstatnieQuizy(potrzebni);

        WierszRankingu Wiersz(int miejsce, Pozycja pozycja) => new()
        {
            Miejsce = miejsce,
            Nick = pozycja.Nick,
            Punkty = pozycja.Punkty,
            Level = pozycja.Level,
            NazwaLevelu = nazwyLeveli.GetValueOrDefault(pozycja.Level),
            SeriaDni = PostepSerwis.AktualnaSeria(pozycja.SeriaDni, ostatnieQuizy.TryGetValue(pozycja.Id, out var quiz) ? quiz : null),
            CzyJa = pozycja.Id == zalogowanyUczenId
        };

        var model = new ModelRankingu { Wiersze = najlepsi.Select((pozycja, indeks) => Wiersz(indeks + 1, pozycja)).ToList() };

        if (zalogowanyUczenId is { } mojId && !model.Wiersze.Any(wiersz => wiersz.CzyJa) && db.Uzytkownicy.Find(mojId) is { } ja)
            model.Moj = Wiersz(MiejsceUcznia(ja), new Pozycja(ja.Id, ja.Nick, ja.Punkty, ja.Level, ja.SeriaDni));
        return model;
    }

    int MiejsceUcznia(Uzytkownik ja) =>
        Uczniowie.Count(inny => inny.Punkty > ja.Punkty
            || (inny.Punkty == ja.Punkty && inny.DataRejestracji < ja.DataRejestracji)
            || (inny.Punkty == ja.Punkty && inny.DataRejestracji == ja.DataRejestracji && inny.Id < ja.Id)) + 1;
}
