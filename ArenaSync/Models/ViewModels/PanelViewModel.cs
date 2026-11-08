namespace ArenaSync.Models.ViewModels
{
    // Dane panelu startowego po zalogowaniu (zawartość zależy od roli)
    public class PanelViewModel
    {
        public string Rola { get; set; } = string.Empty;
        public string? ImieNazwisko { get; set; }

        // Drużyny użytkownika: kapitan - prowadzone, zawodnik - jego drużyna
        public List<Druzyna> MojeDruzyny { get; set; } = new();
        public Mecz? NastepnyMecz { get; set; }
        public List<Mecz> Nadchodzace { get; set; } = new();
        public List<Mecz> OstatnieWyniki { get; set; } = new();
        public List<WierszTabeli> Tabela { get; set; } = new();
        public Turniej? TurniejDruzyny { get; set; }

        // Admin
        public List<Druzyna> Zgloszenia { get; set; } = new();
        public List<Mecz> DoUzupelnienia { get; set; } = new();
        public int LiczbaTurniejow { get; set; }
        public int LiczbaDruzyn { get; set; }
        public int LiczbaZawodnikow { get; set; }
        public int LiczbaUzytkownikow { get; set; }
        public int UwagiNowe { get; set; }
    }
}
