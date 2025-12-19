using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Otzivi.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsVerifiedPurchase { get; set; }


        public bool IsActive { get; set; } = true;
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }


        // Навигационные свойства
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public ICollection<ReviewLike> ReviewLikes { get; set; } = new List<ReviewLike>();

    }
}