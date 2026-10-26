using Liczydelko.Dane;
using Liczydelko.Modele;

namespace Liczydelko.Serwisy;

public class QuizSerwis(KontekstBazy db)
{
    public List<PytanieQuizu> PobierzPytania(int lekcjaId) =>
        db.PytaniaQuizu.Where(pytanie => pytanie.LekcjaId == lekcjaId).OrderBy(pytanie => pytanie.Trudnosc).ThenBy(pytanie => pytanie.Id).ToList();

    public HashSet<int> IdPytan(int lekcjaId, int trudnosc) =>
        db.PytaniaQuizu.Where(pytanie => pytanie.LekcjaId == lekcjaId && pytanie.Trudnosc == trudnosc)
            .Select(pytanie => pytanie.Id).ToHashSet();

    static string Sortuj(string? litery) => string.Concat((litery ?? "").ToUpperInvariant().Distinct().Order());

    public List<Ocena> Sprawdz(List<OdpowiedzDto> odpowiedzi)
    {
        var unikalne = odpowiedzi.DistinctBy(odpowiedz => odpowiedz.PytanieId).ToList();
        var idPytan = unikalne.Select(odpowiedz => odpowiedz.PytanieId).ToList();
        var pytania = db.PytaniaQuizu.Where(pytanie => idPytan.Contains(pytanie.Id)).ToDictionary(pytanie => pytanie.Id);

        return unikalne.Where(odpowiedz => pytania.ContainsKey(odpowiedz.PytanieId)).Select(odpowiedz =>
        {
            var pytanie = pytania[odpowiedz.PytanieId];
            return new Ocena
            {
                Pytanie = pytanie,
                Wybrana = odpowiedz.Wybrana ?? "",
                Poprawna = Sortuj(pytanie.Poprawna) == Sortuj(odpowiedz.Wybrana),
                Czas = Math.Clamp(odpowiedz.Czas, 0, 3600)
            };
        }).ToList();
    }

    public static bool PorownajOdpowiedzi(string? wzor, string? podana) => Normalizuj(wzor) == Normalizuj(podana);

    static string Normalizuj(string? tekst) =>
        (tekst ?? "").ToLowerInvariant().Replace(" ", "").Replace(",", ".").Replace("\u2212", "-");

    public (Zadanie? Zadanie, bool Poprawna) SprawdzZadanie(int id, string? odpowiedz)
    {
        var zadanie = db.Zadania.Find(id);
        if (zadanie == null) return (null, false);

        var wzorce = zadanie.Rodzaj == "Blad" ? zadanie.Odpowiedz.Split('|') : new[] { zadanie.Odpowiedz };
        return (zadanie, wzorce.Any(wzor => PorownajOdpowiedzi(wzor, odpowiedz)));
    }
}
