using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace H_st_2026_Gruppe_1.Models
{
    public class ResourceNeed : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title or short summary is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact information is required.")]
        public string ContactInfo { get; set; } = string.Empty;

        // Design-only fields. They will be mapped to the database after the form design is approved.
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [NotMapped]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please provide a valid phone number.")]
        [NotMapped]
        public string PhoneNumber { get; set; } = "+47 ";

        [Required(ErrorMessage = "Please select a crisis incident.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid crisis incident.")]
        public int CrisisEventId { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category.")]
        public int Category { get; set; }
        public Category? CategoryInfo { get; set; }

        // Required by the database. Set in the controller, not in the form.
        public int PublicActorId { get; set; }
        
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        [DataType(DataType.Date)]
        public DateTime? RequiredFrom { get; set; }

        public NeedPriorityEnum Priority { get; set; } = NeedPriorityEnum.Planned;
        
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        [StringLength(200)]
        [NotMapped]
        public string LocationName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Map location is required.")]
        [Range(-90.0, 90.0, ErrorMessage = "Invalid latitude value.")]
        public double Latitude { get; set; }
        
        public DateTime RegisteredAt { get; set; }

        [Required(ErrorMessage = "Map location is required.")]
        [Range(-180.0, 180.0, ErrorMessage = "Invalid longitude value.")]
        public double Longitude { get; set; }

        public string Status { get; set; } = "new";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RequiredFrom.HasValue && Deadline.HasValue && Deadline < RequiredFrom)
            {
                yield return new ValidationResult(
                    "End date cannot be before start date.",
                    new[] { nameof(Deadline) });
            }
        }
    }
}
