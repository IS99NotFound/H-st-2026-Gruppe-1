using System;
using System.ComponentModel.DataAnnotations;


namespace H_st_2026_Gruppe_1.Models
{
    public class ResourceNeed : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tittel eller kort sammendrag er påkrevd.")]
        [StringLength(100, ErrorMessage = "Tittelen kan ikke være lengre enn 100 tegn.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-post er påkrevd.")]
        [EmailAddress(ErrorMessage = "Oppgi en gyldig e-postadresse.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefonnummer er påkrevd.")]
        [Phone(ErrorMessage = "Oppgi et gyldig telefonnummer.")]
        public string PhoneNumber { get; set; } = "+47 ";

        [Required(ErrorMessage = "Velg en krisehendelse.")]
        [Range(1, int.MaxValue, ErrorMessage = "Velg en gyldig krisehendelse.")]
        public int CrisisEventId { get; set; }

        [Required(ErrorMessage = "Velg en kategori.")]
        [Range(1, int.MaxValue, ErrorMessage = "Velg en gyldig kategori.")]
        public int Category { get; set; }
        public Category? CategoryInfo { get; set; }

        // Required by the database. Set in the controller, not in the form.
        public int PublicActorId { get; set; }
        
        [Required(ErrorMessage = "Beskrivelse er påkrevd.")]
        [StringLength(1000, ErrorMessage = "Beskrivelsen kan ikke være lengre enn 1000 tegn.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Antall er påkrevd.")]
        [Range(1, 10000, ErrorMessage = "Antall må være minst 1.")]
        public int Quantity { get; set; } = 1;

        [DataType(DataType.Date)]
        public DateTime? RequiredFrom { get; set; }

        public NeedPriorityEnum Priority { get; set; } = NeedPriorityEnum.Planned;
        
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        [StringLength(200, ErrorMessage = "Stedsnavn kan ikke være lengre enn 200 tegn.")]
        public string LocationName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Sted på kartet er påkrevd.")]
        [Range(-90.0, 90.0, ErrorMessage = "Ugyldig breddegrad.")]
        public double Latitude { get; set; }
        
        public DateTime RegisteredAt { get; set; }

        [Required(ErrorMessage = "Sted på kartet er påkrevd.")]
        [Range(-180.0, 180.0, ErrorMessage = "Ugyldig lengdegrad.")]
        public double Longitude { get; set; }

        public string Status { get; set; } = "new";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RequiredFrom.HasValue && Deadline.HasValue && Deadline < RequiredFrom)
            {
                yield return new ValidationResult(
                    "Sluttdato kan ikke være før startdato.",
                    new[] { nameof(Deadline) });
            }
        }
    }
}
