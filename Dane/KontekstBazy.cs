using Liczydelko.Modele;
using Microsoft.EntityFrameworkCore;

namespace Liczydelko.Dane;

public class KontekstBazy(DbContextOptions<KontekstBazy> opcje) : DbContext(opcje)
{
    public DbSet<Uzytkownik> Uzytkownicy { get; set; }
    public DbSet<Awatar> Awatary { get; set; }
    public DbSet<PoziomEdukacyjny> PoziomyEdukacyjne { get; set; }
    public DbSet<Dzial> Dzialy { get; set; }
    public DbSet<Lekcja> Lekcje { get; set; }
    public DbSet<Przyklad> Przyklady { get; set; }
    public DbSet<PytanieQuizu> PytaniaQuizu { get; set; }
    public DbSet<Zadanie> Zadania { get; set; }
    public DbSet<WynikCwiczenia> WynikiCwiczen { get; set; }
    public DbSet<Wynik> Wyniki { get; set; }
    public DbSet<OdpowiedzUzytkownika> Odpowiedzi { get; set; }
    public DbSet<PowtorkaBledu> Powtorki { get; set; }
    public DbSet<DefinicjaLevelu> DefinicjeLeveli { get; set; }
    public DbSet<Odznaka> Odznaki { get; set; }
    public DbSet<OdznakaUzytkownika> OdznakiUzytkownikow { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Uzytkownik>().HasIndex(uzytkownik => uzytkownik.Email).IsUnique();
    modelBuilder.Entity<Odznaka>().HasIndex(odznaka => odznaka.Kod).IsUnique();
    modelBuilder.Entity<Awatar>().HasIndex(awatar => awatar.UzytkownikId).IsUnique();
    modelBuilder.Entity<PowtorkaBledu>().HasIndex(powtorka => new { powtorka.UzytkownikId, powtorka.PytanieQuizuId }).IsUnique();
    modelBuilder.Entity<OdznakaUzytkownika>().HasIndex(zdobycie => new { zdobycie.UzytkownikId, zdobycie.OdznakaId }).IsUnique();

    foreach (var typ in modelBuilder.Model.GetEntityTypes().Where(typ => typeof(IMaKod).IsAssignableFrom(typ.ClrType)))
        modelBuilder.Entity(typ.ClrType).HasIndex(nameof(IMaKod.Kod)).IsUnique();

    foreach (var typ in modelBuilder.Model.GetEntityTypes().Where(typ => typ.FindProperty("UzytkownikId") != null))
        modelBuilder.Entity(typ.ClrType).HasOne(typeof(Uzytkownik)).WithMany().HasForeignKey("UzytkownikId");

    modelBuilder.Entity<OdpowiedzUzytkownika>().HasOne<PytanieQuizu>().WithMany().HasForeignKey(odpowiedz => odpowiedz.PytanieQuizuId);
    modelBuilder.Entity<WynikCwiczenia>().HasOne<Zadanie>().WithMany().HasForeignKey(wynik => wynik.ZadanieId);
}
}
