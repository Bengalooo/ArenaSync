using System.Globalization;

namespace ArenaSync.Services
{
    // Wyszukiwanie bez rozróżniania wielkości liter, także dla polskich znaków (SQLite tego nie robi)
    public static class Wyszukiwanie
    {
        private static readonly CompareInfo Pl = new CultureInfo("pl-PL").CompareInfo;

        public static bool Zawiera(string? tekst, string? fraza) =>
            string.IsNullOrWhiteSpace(fraza)
            || (tekst != null && Pl.IndexOf(tekst, fraza.Trim(), CompareOptions.IgnoreCase) >= 0);

        public static bool KtorekolwiekZawiera(string? fraza, params string?[] teksty) =>
            string.IsNullOrWhiteSpace(fraza) || teksty.Any(t => Zawiera(t, fraza));
    }
}
