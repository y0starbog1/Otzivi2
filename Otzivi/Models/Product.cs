using System.ComponentModel.DataAnnotations;

namespace Otzivi.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Brand { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Вычисляемые свойства (ИЗМЕНИТЬ НА ПРОСТЫЕ СВОЙСТВА)
        public double AverageRating
        {
            get
            {
                if (Reviews == null || !Reviews.Any(r => r.IsActive))
                    return 0;
                return Reviews.Where(r => r.IsActive).Average(r => r.Rating);
            }
        }

        public int TotalReviews
        {
            get
            {
                if (Reviews == null)
                    return 0;
                return Reviews.Count(r => r.IsActive);
            }
        }
    }
}