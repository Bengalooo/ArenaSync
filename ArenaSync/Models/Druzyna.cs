using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ArenaSync.Models
{
    public class Druzyna
    {
        [Key]
        public int IdDruzyny { get; set; }

        [Required(ErrorMessage = "Podaj nazwę drużyny.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Nazwa musi mieć od 2 do 60 znaków.")]
        [DisplayName("Nazwa drużyny")]
        public string Nazwa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj miasto.")]
        [StringLength(50)]
        [DisplayName("Miasto")]
        public string Miasto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj imię i nazwisko trenera / kapitana.")]
        [StringLength(80)]
        [DisplayName("Trener / kapitan")]
        public string Trener { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj adres e-mail.")]
        [EmailAddress(ErrorMessage = "Niepoprawny adres e-mail.")]
        [StringLength(100)]
        [DisplayName("E-mail kontaktowy")]
        public string EmailKontaktowy { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Niepoprawny numer telefonu.")]
        [StringLength(20)]
        [DisplayName("Telefon")]
        public string? Telefon { get; set; }

        [DataType(DataType.Date)]
        [DisplayName("Data zgłoszenia")]
        public DateTime DataZgloszenia { get; set; } = DateTime.Today;

        [DisplayName("Zatwierdzona")]
        public bool Zatwierdzona { get; set; }

        // Id użytkownika (kapitana), który zgłosił drużynę - ASP.NET Core Identity
        [ValidateNever]
        public string? KapitanId { get; set; }

        [Required(ErrorMessage = "Wybierz turniej.")]
        [DisplayName("Turniej")]
        public int TurniejId { get; set; }

        [ValidateNever]
        public Turniej? Turniej { get; set; }

        [ValidateNever]
        public ICollection<Zawodnik> Zawodnicy { get; set; } = new List<Zawodnik>();
    }
}
