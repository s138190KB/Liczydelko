namespace Liczydelko.Serwisy;

public static class Czas
{
    static readonly TimeZoneInfo Strefa = TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Warsaw", out var strefa)
        ? strefa
        : TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    public static DateTime Lokalny(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Strefa);

    public static DateOnly Dzien(DateTime utc) => DateOnly.FromDateTime(Lokalny(utc));

    public static DateOnly Dzisiaj => Dzien(DateTime.UtcNow);

    public static DateTime PoczatekDniaUtc(DateOnly dzien) =>
        TimeZoneInfo.ConvertTimeToUtc(dzien.ToDateTime(TimeOnly.MinValue), Strefa);
}
