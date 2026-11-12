using System.ComponentModel.DataAnnotations;

namespace Beerly.Models
{
    public class Beer
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Brewery { get; set; } = string.Empty;

        [Required]
        public string Style {  get; set; } = string.Empty;

        [Required]
        [Range(0.1, 100, ErrorMessage = "ABV must be between 0.1 and 100.")]
        public double AbvPercentage { get; set; }

        [Required]
        [CountryCode]
        public string Country {  get; set; } = string.Empty;

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

    }
}
