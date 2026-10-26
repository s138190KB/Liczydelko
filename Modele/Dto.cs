using System.ComponentModel.DataAnnotations;

namespace Liczydelko.Modele;

public class OdpowiedzDto
{
    public int PytanieId { get; set; }
    [StringLength(8)] public string? Wybrana { get; set; }
    [Range(0, int.MaxValue)] public int Czas { get; set; }
}

public class ZakonczDto
{
    [Range(0, int.MaxValue)] public int LekcjaId { get; set; }
    [Range(0, 3)] public int Trudnosc { get; set; }
    [Required, MinLength(1), MaxLength(200)] public List<OdpowiedzDto> Odpowiedzi { get; set; } = new();
}

public class SprawdzDto
{
    [Range(1, int.MaxValue)] public int ZadanieId { get; set; }
    [StringLength(200)] public string? Odpowiedz { get; set; }
}

public class LogowanieModel
{
    [Required, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(100)] public string Haslo { get; set; } = "";
}

public class RejestracjaModel
{
    const string BladDanych = "Podaj e-mail, nick i hasło (min. 6 znaków).";
    const string BladDlugosci = "E-mail (maks. 254 znaki) lub nick (maks. 30 znaków) jest za długi.";

    [Required(ErrorMessage = BladDanych), StringLength(254, ErrorMessage = BladDlugosci)]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = BladDanych), StringLength(30, ErrorMessage = BladDlugosci)]
    public string Nick { get; set; } = "";

    [Required(ErrorMessage = BladDanych), StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć od 6 do 100 znaków.")]
    public string Haslo { get; set; } = "";
}

public class ZmianaHaslaModel
{
    [Required(ErrorMessage = "Podaj aktualne hasło.")]
    public string Stare { get; set; } = "";

    [Required(ErrorMessage = "Nowe hasło musi mieć co najmniej 6 znaków.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Nowe hasło musi mieć od 6 do 100 znaków.")]
    public string Nowe { get; set; } = "";

    [Compare(nameof(Nowe), ErrorMessage = "Nowe hasła nie są takie same.")]
    public string Powtorz { get; set; } = "";
}

public class UsuniecieKontaModel
{
    [Required(ErrorMessage = "Podaj hasło, aby potwierdzić usunięcie konta.")]
    public string Haslo { get; set; } = "";
}

public class Ocena
{
    public required PytanieQuizu Pytanie { get; set; }
    public required string Wybrana { get; set; }
    public bool Poprawna { get; set; }
    public int Czas { get; set; }
}

public class ModelLekcji
{
    public required Lekcja Lekcja { get; set; }
    public PostepLekcji? Postep { get; set; }
    public List<PytanieQuizu> Pytania { get; set; } = new();
    public List<Zadanie> Zadania { get; set; } = new();
    public int Polecana { get; set; } = 1;
    public string? PowodPolecenia { get; set; }
}

public class QuizModel
{
    public int LekcjaId { get; set; }
    public List<PytanieQuizu> Pytania { get; set; } = new();
    public bool Trudnosci { get; set; }
    public int Polecany { get; set; } = 1;
    public string? Powod { get; set; }
}

public record PostepLekcji(HashSet<int> ZaliczoneTrudnosci, List<int> WymaganeTrudnosci, int ZadaniaPoprawne, int ZadaniaRazem);

public record Rekomendacja(string Tekst, int? LekcjaId, bool Powtorka);

public record StanLevelu(int Numer, string Nazwa, string Opis, int Wymagane, int Zrobione)
{
    public int Procent => Wymagane == 0 ? 0 : 100 * Zrobione / Wymagane;
}

public class WierszOdznaki
{
    public required string Nazwa { get; set; }
    public required string Opis { get; set; }
    public required string Ikona { get; set; }
    public bool Zdobyta { get; set; }
    public DateTime? Data { get; set; }
}

public class ModelPanelu
{
    public required Uzytkownik Uzytkownik { get; set; }
    public bool MaAwatar { get; set; }
    public int SeriaDni { get; set; }
    public required DefinicjaLevelu Aktualny { get; set; }
    public List<StanLevelu> Leveli { get; set; } = new();
    public required Rekomendacja Rekomendacja { get; set; }
    public int Zaliczone { get; set; }
    public int Wszystkie { get; set; }
    public int Powtorki { get; set; }
    public List<WierszOdznaki> Odznaki { get; set; } = new();
}

public class WpisRaportu
{
    public required string Zrodlo { get; set; }
    public required string Tresc { get; set; }
    public required string Odpowiedz { get; set; }
    public required string Lekcja { get; set; }
    public int LekcjaId { get; set; }
    public required string Kategoria { get; set; }
    public bool CzyPoprawna { get; set; }
}

public class WierszKategorii
{
    public required string Klucz { get; set; }
    public required string Nazwa { get; set; }
    public int Poprawne { get; set; }
    public int Bledne { get; set; }
    public int LekcjiRazem { get; set; }
    public int LekcjiZaliczonych { get; set; }
    public int Ukonczenie => LekcjiRazem == 0 ? 0 : 100 * LekcjiZaliczonych / LekcjiRazem;
    public int Poprawnosc => Poprawne + Bledne == 0 ? 100 : 100 * Poprawne / (Poprawne + Bledne);
}

public class WynikQuizuWiersz
{
    public required string Lekcja { get; set; }
    public int Trudnosc { get; set; }
    public int Punkty { get; set; }
    public int Razem { get; set; }
    public DateTime Data { get; set; }
}

public class RaportDto
{
    public int Poprawne { get; set; }
    public int Bledne { get; set; }
    public int DoPowtorki { get; set; }
    public int Zaplanowane { get; set; }
    public int UkonczoneLekcje { get; set; }
    public int WszystkieLekcje { get; set; }
    public int Ukonczenie => WszystkieLekcje == 0 ? 0 : 100 * UkonczoneLekcje / WszystkieLekcje;
    public List<WierszKategorii> Kategorie { get; set; } = new();
    public List<WpisRaportu> PoprawneWpisy { get; set; } = new();
    public List<WpisRaportu> BledneWpisy { get; set; } = new();
    public List<WynikQuizuWiersz> Quizy { get; set; } = new();
    public List<Rekomendacja> Sugestie { get; set; } = new();
}

public class DzienWykresu
{
    public required string Etykieta { get; set; }
    public int Razem { get; set; }
    public int Poprawne { get; set; }
    public int? Procent { get; set; }
}

public class WierszRankingu
{
    public int Miejsce { get; set; }
    public required string Nick { get; set; }
    public int Punkty { get; set; }
    public int Level { get; set; }
    public string? NazwaLevelu { get; set; }
    public int SeriaDni { get; set; }
    public bool CzyJa { get; set; }
}

public class ModelRankingu
{
    public List<WierszRankingu> Wiersze { get; set; } = new();
    public WierszRankingu? Moj { get; set; }
}
