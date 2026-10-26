using Liczydelko.Dane;
using Liczydelko.Modele;
using Microsoft.EntityFrameworkCore;

namespace Liczydelko.Serwisy;

public class AwatarSerwis(KontekstBazy db)
{
    const int MaksymalnyRozmiar = 1_000_000;

    public bool MaAwatar(int uczenId) => db.Awatary.Any(awatar => awatar.UzytkownikId == uczenId);

    public Task<bool> MaAwatarAsync(int uczenId) => db.Awatary.AnyAsync(awatar => awatar.UzytkownikId == uczenId);

    public Task<Awatar?> PobierzAsync(int uczenId) => db.Awatary.FirstOrDefaultAsync(awatar => awatar.UzytkownikId == uczenId);

    public Task UsunAsync(int uczenId) => db.Awatary.Where(awatar => awatar.UzytkownikId == uczenId).ExecuteDeleteAsync();

    public async Task<string?> ZapiszAsync(int uczenId, IFormFile? plik)
    {
        if (plik == null || plik.Length == 0 || plik.Length > MaksymalnyRozmiar)
            return "Wybierz obraz o rozmiarze do 1 MB.";

        var dane = new byte[plik.Length];
        await using (var strumien = plik.OpenReadStream())
            await strumien.ReadExactlyAsync(dane);

        var typ = TypObrazu(dane);
        if (typ == null) return "Dozwolone formaty: PNG, JPG, GIF i WEBP.";

        var awatar = await PobierzAsync(uczenId) ?? db.Awatary.Add(new Awatar { UzytkownikId = uczenId }).Entity;
        awatar.Dane = dane;
        awatar.Typ = typ;
        await db.SaveChangesAsync();
        return null;
    }

    static string? TypObrazu(ReadOnlySpan<byte> dane) =>
        dane.StartsWith<byte>([0x89, 0x50, 0x4E, 0x47]) ? "image/png"
        : dane.StartsWith<byte>([0xFF, 0xD8, 0xFF]) ? "image/jpeg"
        : dane.StartsWith("GIF"u8) ? "image/gif"
        : dane.StartsWith("RIFF"u8) && dane.Length >= 12 && dane[8..12].SequenceEqual("WEBP"u8) ? "image/webp"
        : null;
}
