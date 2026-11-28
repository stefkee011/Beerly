using System.ComponentModel.DataAnnotations;

namespace Beerly.Models
{
    public class CreateBeerRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Brewery { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Style { get; set; } = string.Empty;

        [Range(0.1, 100)]
        public double AbvPercentage { get; set; }

        [Required, CountryCode]
        public string Country { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        [RegularExpression(@"^/uploads/beers/[0-9a-f]{32}\.webp$", ErrorMessage = "Image must be uploaded through the app.")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
