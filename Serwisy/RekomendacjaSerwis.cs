using Liczydelko.Dane;
using Liczydelko.Modele;

namespace Liczydelko.Serwisy;

public class RekomendacjaSerwis(KontekstBazy db, PostepSerwis postep)
{
    const int OknoOdpowiedzi = 20;
    const int ProgBledow = 3;
    const int ProgPowtorek = 3;
    const int MinOdpowiedzi = 5;

    public Rekomendacja Zalec(Uzytkownik uczen)
    {
        var lekcje = db.Lekcje.OrderBy(lekcja => lekcja.DzialId).ThenBy(lekcja => lekcja.Kolejnosc)
            .Select(lekcja => new { lekcja.Id, lekcja.Tytul, Kategoria = lekcja.Dzial.Kategoria }).ToList();
        var zaliczone = postep.Zaliczone(uczen.Id);
        var bledy = BledyWKategoriach(uczen.Id, lekcje.ToDictionary(lekcja => lekcja.Id, lekcja => lekcja.Kategoria));

        foreach (var (kategoria, liczba) in bledy.Where(wpis => wpis.Value > ProgBledow).OrderByDescending(wpis => wpis.Value))
        {
            var polecana = lekcje.FirstOrDefault(lekcja => lekcja.Kategoria == kategoria && !zaliczone.Contains(lekcja.Id));
            if (polecana != null)
                return new($"Masz dużo błędów w kategorii {AnalizaBledowSerwis.Etykieta(kategoria)} ({liczba}). Polecamy lekcję: {polecana.Tytul}.", polecana.Id, false);
        }

        var teraz = DateTime.UtcNow;
        if (db.Powtorki.Count(powtorka => powtorka.UzytkownikId == uczen.Id && powtorka.NastepnaPowtorka <= teraz) >= ProgPowtorek)
            return new("Zebrało się kilka błędnych pytań. Czas na powtórkę.", null, true);

        var nastepna = lekcje.FirstOrDefault(lekcja => !zaliczone.Contains(lekcja.Id));
        return nastepna != null
            ? new($"Następna lekcja: {nastepna.Tytul}.", nastepna.Id, false)
            : new("Wszystkie dostępne lekcje są zaliczone. Nowe materiały pojawią się wkrótce.", null, false);
    }

    public (int Trudnosc, string? Powod) PolecanaTrudnosc(int uczenId, int lekcjaId)
    {
        var postepLekcji = postep.PobierzPostepLekcji(uczenId, lekcjaId);
        var dostepne = postepLekcji.WymaganeTrudnosci;
        if (dostepne.Count == 0) return (1, null);

        var kategoria = db.Lekcje.Where(lekcja => lekcja.Id == lekcjaId).Select(lekcja => lekcja.Dzial.Kategoria).FirstOrDefault();
        var odpowiedzi = (from odpowiedz in db.Odpowiedzi
                          where odpowiedz.UzytkownikId == uczenId
                          join pytanie in db.PytaniaQuizu on odpowiedz.PytanieQuizuId equals pytanie.Id
                          join lekcja in db.Lekcje on pytanie.LekcjaId equals lekcja.Id
                          where lekcja.Dzial.Kategoria == kategoria
                          select odpowiedz.CzyPoprawna).ToList();

        int? skutecznosc = odpowiedzi.Count >= MinOdpowiedzi ? 100 * odpowiedzi.Count(poprawna => poprawna) / odpowiedzi.Count : null;
        var cel = skutecznosc switch { >= 85 => 3, >= 65 => 2, _ => 1 };

        var kandydaci = dostepne.Except(postepLekcji.ZaliczoneTrudnosci).ToList();
        if (kandydaci.Count == 0) kandydaci = dostepne;
        var wybrany = kandydaci.MinBy(trudnosc => (Math.Abs(trudnosc - cel), trudnosc));

        return (wybrany, skutecznosc is { } procent
            ? $"Dobrano do Twojej skuteczności w tym dziale ({procent}%)."
            : "W tym dziale masz jeszcze mało odpowiedzi, więc poziom jest dobrany ostrożnie.");
    }

    Dictionary<string, int> BledyWKategoriach(int uczenId, Dictionary<int, string> kategorieLekcji)
    {
        var quiz = db.Odpowiedzi.Where(odpowiedz => odpowiedz.UzytkownikId == uczenId)
            .Join(db.PytaniaQuizu, odpowiedz => odpowiedz.PytanieQuizuId, pytanie => pytanie.Id,
                (odpowiedz, pytanie) => new { odpowiedz.Data, odpowiedz.CzyPoprawna, pytanie.LekcjaId }).ToList();
        var zadania = db.WynikiCwiczen.Where(wynik => wynik.UzytkownikId == uczenId)
            .Join(db.Zadania, wynik => wynik.ZadanieId, zadanie => zadanie.Id,
                (wynik, zadanie) => new { wynik.Data, wynik.CzyPoprawna, zadanie.LekcjaId }).ToList();
        return quiz.Concat(zadania)
            .GroupBy(wpis => kategorieLekcji[wpis.LekcjaId])
            .ToDictionary(grupa => grupa.Key,
                grupa => grupa.OrderByDescending(wpis => wpis.Data).Take(OknoOdpowiedzi).Count(wpis => !wpis.CzyPoprawna));
    }
}
