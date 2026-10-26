using Liczydelko.Modele;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Liczydelko.Serwisy;

public static class HaslaSerwis
{
    static readonly PasswordHasher<Uzytkownik> hasher =
        new(Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));
    static readonly Uzytkownik pusty = new();

    static readonly string falszywyHasz = hasher.HashPassword(pusty, "udawane-haslo");

    public static string Haszuj(string haslo) => hasher.HashPassword(pusty, haslo);

    public static bool Sprawdz(string haslo, string zapis) => Sprawdz(haslo, zapis, out _);

    public static bool Sprawdz(string haslo, string zapis, out bool przeliczyc)
    {
        przeliczyc = false;
        if (string.IsNullOrEmpty(zapis)) return false;

        var wynik = hasher.VerifyHashedPassword(pusty, zapis, haslo);
        przeliczyc = wynik == PasswordVerificationResult.SuccessRehashNeeded;
        return wynik != PasswordVerificationResult.Failed;
    }

    public static void UdawajSprawdzenie(string haslo) => hasher.VerifyHashedPassword(pusty, falszywyHasz, haslo);

}
