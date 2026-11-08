using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ArenaSync.Models
{
    public class Mecz : IValidatableObject
    {
        [Key]
        public int IdMeczu { get; set; }

        [Required(ErrorMessage = "Wybierz turniej.")]
        [DisplayName("Turniej")]
        public int TurniejId { get; set; }

        [ValidateNever]
        public Turniej? Turniej { get; set; }

        [Required(ErrorMessage = "Wybierz gospodarza.")]
        [DisplayName("Gospodarz")]
        public int GospodarzId { get; set; }

        [ValidateNever]
        public Druzyna? Gospodarz { get; set; }

        [Required(ErrorMessage = "Wybierz gościa.")]
        [DisplayName("Gość")]
        public int GoscId { get; set; }

        [ValidateNever]
        public Druzyna? Gosc { get; set; }

        [Required(ErrorMessage = "Podaj termin meczu.")]
        [DataType(DataType.DateTime)]
        [DisplayName("Termin")]
        public DateTime Termin { get; set; } = DateTime.Today.AddHours(12);

        [StringLength(60)]
        [DisplayName("Boisko")]
        public string? Boisko { get; set; }

        [Range(0, 99, ErrorMessage = "Wartość 0–99.")]
        [DisplayName("Bramki gospodarza")]
        public int? BramkiGospodarza { get; set; }

        [Range(0, 99, ErrorMessage = "Wartość 0–99.")]
        [DisplayName("Bramki gościa")]
        public int? BramkiGoscia { get; set; }

        [DisplayName("Rozegrany")]
        public bool Rozegrany => BramkiGospodarza.HasValue && BramkiGoscia.HasValue;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (GospodarzId == GoscId)
                yield return new ValidationResult("Drużyna nie może grać sama ze sobą.", new[] { nameof(GoscId) });
            if (BramkiGospodarza.HasValue != BramkiGoscia.HasValue)
                yield return new ValidationResult("Podaj wynik obu drużyn albo zostaw oba pola puste.", new[] { nameof(BramkiGoscia) });
        }
    }
}
