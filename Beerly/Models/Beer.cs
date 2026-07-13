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

        public double? AbvPercentage { get; set; }

        [Required]
        public string Country {  get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
