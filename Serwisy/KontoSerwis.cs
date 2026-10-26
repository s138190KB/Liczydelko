using Liczydelko.Dane;
using Liczydelko.Modele;
using Microsoft.EntityFrameworkCore;

namespace Liczydelko.Serwisy;

public class KontoSerwis(KontekstBazy db)
{
    const int MaksymalnaLiczbaProb = 5;
    const int MinutBlokady = 15;

    public static string NormalizujEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    public async Task<Uzytkownik?> ZalogujAsync(string? email, string? haslo)
    {
        email = NormalizujEmail(email);
        haslo ??= "";
        var uczen = await db.Uzytkownicy.FirstOrDefaultAsync(uzytkownik => uzytkownik.Email == email);

        if (uczen == null || uczen.BlokadaDo > DateTime.UtcNow)
        {
            HaslaSerwis.UdawajSprawdzenie(haslo);
            return null;
        }
        if (uczen.BlokadaDo != null)
        {
            uczen.BlokadaDo = null;
            uczen.NieudaneProby = 0;
        }

        if (!HaslaSerwis.Sprawdz(haslo, uczen.Haslo, out var przeliczyc))
        {
            if (++uczen.NieudaneProby >= MaksymalnaLiczbaProb)
            {
                uczen.BlokadaDo = DateTime.UtcNow.AddMinutes(MinutBlokady);
                uczen.NieudaneProby = 0;
            }
            await db.SaveChangesAsync();
            return null;
        }

        uczen.NieudaneProby = 0;
        if (przeliczyc) uczen.Haslo = HaslaSerwis.Haszuj(haslo);
        await db.SaveChangesAsync();
        return uczen;
    }

    public async Task<Uzytkownik?> ZarejestrujAsync(string email, string haslo, string nick)
    {
        email = NormalizujEmail(email);
        if (await db.Uzytkownicy.AnyAsync(uzytkownik => uzytkownik.Email == email)) return null;

        var uczen = new Uzytkownik { Email = email, Haslo = HaslaSerwis.Haszuj(haslo), Nick = nick.Trim() };
        db.Uzytkownicy.Add(uczen);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await db.Uzytkownicy.AnyAsync(uzytkownik => uzytkownik.Email == email)) return null;
            throw;
        }
        return uczen;
    }

    public async Task<bool> ZmienHasloAsync(int uczenId, string stareHaslo, string noweHaslo)
    {
        var uczen = await ZnajdzZHaslem(uczenId, stareHaslo);
        if (uczen == null) return false;
        uczen.Haslo = HaslaSerwis.Haszuj(noweHaslo);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UsunKontoAsync(int uczenId, string haslo)
    {
        var uczen = await ZnajdzZHaslem(uczenId, haslo);
        if (uczen == null) return false;
        db.Uzytkownicy.Remove(uczen);
        await db.SaveChangesAsync();
        return true;
    }

    async Task<Uzytkownik?> ZnajdzZHaslem(int uczenId, string haslo)
    {
        var uczen = await db.Uzytkownicy.FindAsync(uczenId);
        return uczen != null && HaslaSerwis.Sprawdz(haslo, uczen.Haslo) ? uczen : null;
    }
}
