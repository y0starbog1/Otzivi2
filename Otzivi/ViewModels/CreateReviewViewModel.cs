using System.ComponentModel.DataAnnotations;

namespace Otzivi.ViewModels
{
    public class CreateReviewViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Пожалуйста, выберите рейтинг")]
        [Range(1, 5, ErrorMessage = "Рейтинг должен быть от 1 до 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Введите заголовок отзыва")]
        [StringLength(100, ErrorMessage = "Заголовок не должен превышать 100 символов")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Введите текст отзыва")]
        [StringLength(1000, ErrorMessage = "Текст отзыва не должен превышать 1000 символов")]
        public string Content { get; set; }

        public bool IsVerifiedPurchase { get; set; }
    }
}