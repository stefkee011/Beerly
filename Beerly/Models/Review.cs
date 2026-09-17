using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Beerly.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        [CustomValidation(typeof(Review), nameof(ValidateRating))]
        public double Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public int BeerId { get; set; }
        [ForeignKey(nameof(BeerId))]
        public Beer? Beer { get; set; }

        public static ValidationResult? ValidateRating(double rating, ValidationContext context)
        {
            var doubled = rating * 2;
            if (doubled != Math.Floor(doubled))
            {
                return new ValidationResult("Rating must be in increments of 0.5 (e.g. 1, 1.5, 2, 2.5...).");
            }
            return ValidationResult.Success;
        }
    }
}