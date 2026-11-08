namespace ArenaSync.Services
{
    // Wspólne wymagania dotyczące haseł: te same w Identity (Program.cs), w walidacji formularzy i w podpowiedzi na stronie
    public static class Hasla
    {
        public const int MinDlugosc = 10;

        // Co najmniej 10 znaków, mała i wielka litera, cyfra i znak specjalny (polskie litery liczą się jako litery)
        public const string Wzorzec =
            @"(?=.*[a-ząćęłńóśźż])(?=.*[A-ZĄĆĘŁŃÓŚŹŻ])(?=.*\d)(?=.*[^A-Za-z0-9ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]).{10,100}";

        public const string Komunikat =
            "Hasło musi mieć co najmniej 10 znaków, w tym małą i wielką literę, cyfrę i znak specjalny.";

        private const string Male = "abcdefghjkmnpqrstuvwxyz";
        private const string Wielkie = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Cyfry = "23456789";
        private const string Specjalne = "!@#$%*?-_+";

        // Hasło tymczasowe dla resetu przez administratora: 12 znaków, bez mylących znaków (l, 1, O, 0)
        public static string Tymczasowe()
        {
            string wszystkie = Male + Wielkie + Cyfry + Specjalne;
            var znaki = new List<char> { Losuj(Male), Losuj(Wielkie), Losuj(Cyfry), Losuj(Specjalne) };
            while (znaki.Count < 12) znaki.Add(Losuj(wszystkie));
            // Tasowanie, żeby wymagane znaki nie stały zawsze na początku
            return string.Concat(znaki.OrderBy(_ => System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue)));
        }

        private static char Losuj(string zbior) => zbior[System.Security.Cryptography.RandomNumberGenerator.GetInt32(zbior.Length)];
    }
}
