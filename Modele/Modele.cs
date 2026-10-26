namespace Liczydelko.Modele;

public static class Role
{
    public const string Uczen = "uczen";
}

public interface IMaKod
{
    string? Kod { get; set; }
}

public class Uzytkownik
{
    public const int Level1 = 1;

    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Haslo { get; set; } = "";
    public string Nick { get; set; } = "";
    public int Punkty { get; set; }
    public int Level { get; set; } = Level1;
    public int SeriaDni { get; set; }
    public DateTime DataRejestracji { get; set; } = DateTime.UtcNow;
    public string Rola { get; set; } = Role.Uczen;
    public int NieudaneProby { get; set; }
    public DateTime? BlokadaDo { get; set; }
}

public class Awatar
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public byte[] Dane { get; set; } = [];
    public string Typ { get; set; } = "";
}

public class PoziomEdukacyjny : IMaKod
{
    public int Id { get; set; }
    public string? Kod { get; set; }
    public string Nazwa { get; set; } = "";
    public string TypSzkoly { get; set; } = "";
    public string Zakres { get; set; } = "";
    public int Kolejnosc { get; set; }
    public string Opis { get; set; } = "";
    public List<Dzial> Dzialy { get; set; } = [];
}

public class Dzial : IMaKod
{
    public int Id { get; set; }
    public string? Kod { get; set; }
    public string Nazwa { get; set; } = "";
    public string Opis { get; set; } = "";
    public string Kategoria { get; set; } = "";
    public int PoziomEdukacyjnyId { get; set; }
    public PoziomEdukacyjny PoziomEdukacyjny { get; set; } = null!;
    public List<Lekcja> Lekcje { get; set; } = [];
}

public class Lekcja : IMaKod
{
    public int Id { get; set; }
    public string? Kod { get; set; }
    public string Tytul { get; set; } = "";
    public string Wprowadzenie { get; set; } = "";
    public string Teoria { get; set; } = "";
    public string Wzory { get; set; } = "";
    public string Wskazowki { get; set; } = "";
    public int DzialId { get; set; }
    public Dzial Dzial { get; set; } = null!;
    public int Kolejnosc { get; set; }
    public int WymaganyWynik { get; set; }
    public List<Przyklad> Przyklady { get; set; } = [];
    public List<PytanieQuizu> Pytania { get; set; } = [];
    public List<Zadanie> Zadania { get; set; } = [];
}

public class Przyklad : IMaKod
{
    public int Id { get; set; }
    public string? Kod { get; set; }
    public string Tresc { get; set; } = "";
    public string Rozwiazanie { get; set; } = "";
    public int LekcjaId { get; set; }
}

public class PytanieQuizu : IMaKod
{
    public int Id { get; set; }
    public string? Kod { get; set; }
    public string Tresc { get; set; } = "";
    public string OdpA { get; set; } = "";
    public string OdpB { get; set; } = "";
    public string OdpC { get; set; } = "";
    public string OdpD { get; set; } = "";
    public string Poprawna { get; set; } = "";
    public int Trudnosc { get; set; } = 1;
    public int LekcjaId { get; set; }
}

public class Zadanie : IMaKod
{
    public int Id { get; set; }
    public string? Kod { get; set; }
    public int LekcjaId { get; set; }
    public string Rodzaj { get; set; } = "";
    public string Tresc { get; set; } = "";
    public string Odpowiedz { get; set; } = "";
    public string? BledneRozwiazanie { get; set; }
    public string? Wyjasnienie { get; set; }
}

public class WynikCwiczenia
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public int ZadanieId { get; set; }
    public bool CzyPoprawna { get; set; }
    public DateTime Data { get; set; }
}

public class Wynik
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public int LekcjaId { get; set; }
    public Lekcja Lekcja { get; set; } = null!;
    public int LiczbaPunktow { get; set; }
    public int Razem { get; set; }
    public int Trudnosc { get; set; }
    public DateTime Data { get; set; }
}

public class OdpowiedzUzytkownika
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public int PytanieQuizuId { get; set; }
    public string WybranaOdp { get; set; } = "";
    public bool CzyPoprawna { get; set; }
    public int CzasSekundy { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
}

public class PowtorkaBledu
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public int PytanieQuizuId { get; set; }
    public PytanieQuizu PytanieQuizu { get; set; } = null!;
    public int Pudelko { get; set; } = 1;
    public DateTime NastepnaPowtorka { get; set; } = DateTime.UtcNow;
}

public class DefinicjaLevelu
{
    public int Id { get; set; }
    public int Numer { get; set; }
    public string Nazwa { get; set; } = "";
    public string Opis { get; set; } = "";
    public int WymaganyEtap { get; set; }
}

public class Odznaka
{
    public int Id { get; set; }
    public string Kod { get; set; } = "";
    public string Nazwa { get; set; } = "";
    public string Opis { get; set; } = "";
    public string Ikona { get; set; } = "";
}

public class OdznakaUzytkownika
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public int OdznakaId { get; set; }
    public Odznaka Odznaka { get; set; } = null!;
    public DateTime DataZdobycia { get; set; }
}
