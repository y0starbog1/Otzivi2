// Models/LoginWithCaptchaInputModel.cs
using System.ComponentModel.DataAnnotations;

namespace Otzivi.Models
{
    public class LoginWithCaptchaInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        [Required(ErrorMessage = "Введите код с картинки")]
        [Display(Name = "Код с картинки")]
        public string CaptchaCode { get; set; }
    }
}