// 📁 RegisterViewModel.cs - ИЗМЕНИТЕ НАЗВАНИЕ ФАЙЛА И КЛАССА
using System.ComponentModel.DataAnnotations;

namespace Otzivi.ViewModels
{
    // ИЗМЕНИТЕ НАЗВАНИЕ КЛАССА
    public class AccountRegisterViewModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Код капчи обязателен")]
        [Display(Name = "Код с картинки")]
        public string CaptchaCode { get; set; }
    }

    // И ЭТОТ ТОЖЕ ПЕРЕИМЕНУЙТЕ
    public class AccountLoginViewModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }

        [Required(ErrorMessage = "Код капчи обязателен")]
        public string CaptchaCode { get; set; }

        public string ReturnUrl { get; set; }
    }
}