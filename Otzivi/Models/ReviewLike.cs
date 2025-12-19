using System.ComponentModel.DataAnnotations.Schema;

namespace Otzivi.Models
{
    public class ReviewLike
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public string UserId { get; set; }
        public bool IsLike { get; set; } // true = like, false = dislike
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        [ForeignKey("ReviewId")]
        public Review Review { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}