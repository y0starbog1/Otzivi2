using System.ComponentModel.DataAnnotations;

namespace Otzivi.ViewModels
{
    public class VerifySecurityQuestionViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string SecurityQuestion { get; set; }

        [Required(ErrorMessage = "Введите ответ на секретный вопрос")]
        [Display(Name = "Ответ")]
        public string SecurityAnswer { get; set; }

        public string ReturnUrl { get; set; }  // 👈 УБРАЛИ [Required]
        public bool RememberMe { get; set; }
    }
}