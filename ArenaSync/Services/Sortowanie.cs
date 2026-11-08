using System.Globalization;

namespace ArenaSync.Services
{
    // Sortowanie list po kluczu z adresu (?sort=...&kier=asc|desc); nieznany klucz = kolejność domyślna.
    // Sortuje w pamięci z polską kolejnością liter (Ł po L, Ś po S), czego SQLite sam nie robi.
    public static class Sortowanie
    {
        private static readonly IComparer<object?> Porownanie = new PolskiePorownanie();

        public static List<T> Sortuj<T>(IEnumerable<T> lista, string? sort, string? kier,
            Dictionary<string, Func<T, object?>> kolumny)
        {
            if (string.IsNullOrEmpty(sort) || !kolumny.TryGetValue(sort.ToLowerInvariant(), out var klucz))
                return lista.ToList();
            return (kier == "desc" ? lista.OrderByDescending(klucz, Porownanie) : lista.OrderBy(klucz, Porownanie)).ToList();
        }

        private class PolskiePorownanie : IComparer<object?>
        {
            private static readonly CompareInfo Pl = new CultureInfo("pl-PL").CompareInfo;

            public int Compare(object? x, object? y)
            {
                if (x is string a && y is string b) return Pl.Compare(a, b, CompareOptions.IgnoreCase);
                if (x == null) return y == null ? 0 : -1;
                if (y == null) return 1;
                return Comparer<object>.Default.Compare(x, y);
            }
        }
    }
}
