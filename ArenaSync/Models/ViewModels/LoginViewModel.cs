using ArenaSync.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ArenaSync.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Podaj e-mail.")]
        [EmailAddress(ErrorMessage = "Niepoprawny adres e-mail.")]
        [DisplayName("E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj hasło.")]
        [DataType(DataType.Password)]
        [DisplayName("Hasło")]
        public string Haslo { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Podaj imię i nazwisko.")]
        [StringLength(80, MinimumLength = 3, ErrorMessage = "Od 3 do 80 znaków.")]
        [DisplayName("Imię i nazwisko")]
        public string ImieNazwisko { get; set; } = string.Empty;

        // Kapitan albo Zawodnik - konta administratora nie da się założyć samemu
        [Required(ErrorMessage = "Wybierz rodzaj konta.")]
        [DisplayName("Rodzaj konta")]
        public string Rola { get; set; } = "Zawodnik";

        [DisplayName("Moja drużyna")]
        public int? DruzynaId { get; set; }

        [Required(ErrorMessage = "Podaj e-mail.")]
        [EmailAddress(ErrorMessage = "Niepoprawny adres e-mail.")]
        [DisplayName("E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj hasło.")]
        [RegularExpression(Hasla.Wzorzec, ErrorMessage = Hasla.Komunikat)]
        [DataType(DataType.Password)]
        [DisplayName("Hasło")]
        public string Haslo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Powtórz hasło.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Haslo), ErrorMessage = "Hasła nie są identyczne.")]
        [DisplayName("Powtórz hasło")]
        public string PowtorzHaslo { get; set; } = string.Empty;
    }
}

namespace ArenaSync.Models.ViewModels
{
    public class UstawieniaViewModel
    {
        [Required(ErrorMessage = "Podaj imię i nazwisko.")]
        [StringLength(80, MinimumLength = 3, ErrorMessage = "Od 3 do 80 znaków.")]
        [DisplayName("Imię i nazwisko")]
        public string ImieNazwisko { get; set; } = string.Empty;
    }

    public class ZmianaHaslaViewModel
    {
        [Required(ErrorMessage = "Podaj obecne hasło.")]
        [DataType(DataType.Password)]
        [DisplayName("Obecne hasło")]
        public string ObecneHaslo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj nowe hasło.")]
        [RegularExpression(Hasla.Wzorzec, ErrorMessage = Hasla.Komunikat)]
        [DataType(DataType.Password)]
        [DisplayName("Nowe hasło")]
        public string NoweHaslo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Powtórz nowe hasło.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NoweHaslo), ErrorMessage = "Hasła nie są identyczne.")]
        [DisplayName("Powtórz nowe hasło")]
        public string PowtorzHaslo { get; set; } = string.Empty;
    }

    public class UzytkownikViewModel
    {
        public string Id { get; set; } = string.Empty;

        [DisplayName("E-mail")]
        public string Email { get; set; } = string.Empty;

        [StringLength(80)]
        [DisplayName("Imię i nazwisko")]
        public string? ImieNazwisko { get; set; }

        [Required]
        [DisplayName("Rola")]
        public string Rola { get; set; } = "Zawodnik";

        [DisplayName("Drużyna (zawodnika)")]
        public int? DruzynaId { get; set; }

        public string? NazwaDruzyny { get; set; }
        public List<string> ProwadzoneDruzyny { get; set; } = new();

        // Bezpieczeństwo konta
        public bool Zablokowane { get; set; }
        public bool BlokadaWlaczona { get; set; }
        public int NieudaneProby { get; set; }
    }

    public class ResetHaslaViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [RegularExpression(Hasla.Wzorzec, ErrorMessage = Hasla.Komunikat)]
        [DataType(DataType.Password)]
        [DisplayName("Nowe hasło")]
        public string? NoweHaslo { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NoweHaslo), ErrorMessage = "Hasła nie są identyczne.")]
        [DisplayName("Powtórz nowe hasło")]
        public string? PowtorzHaslo { get; set; }
    }
}
