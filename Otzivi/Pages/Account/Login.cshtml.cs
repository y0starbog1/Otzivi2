using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otzivi.Services;

namespace Otzivi.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SimpleCaptchaService _captchaService;

        public LoginModel(SimpleCaptchaService captchaService)
        {
            _captchaService = captchaService;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public bool RememberMe { get; set; }

        [BindProperty]
        public string CaptchaCode { get; set; } // Добавляем свойство для капчи

        public string ReturnUrl { get; set; }

        public IActionResult OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            return Page();
        }

        public IActionResult OnPost(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            // 1. Проверяем капчу
            if (!_captchaService.ValidateCaptcha(CaptchaCode))
            {
                ModelState.AddModelError("CaptchaCode", "Неверный код с картинки");
                return Page();
            }

            // 2. Простая валидация
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError("", "Введите email и пароль");
                return Page();
            }

            // 3. Перенаправляем в LoginController с капчей
            return RedirectToAction("Login", "Login", new
            {
                email = Email,
                password = Password,
                rememberMe = RememberMe,
                captchaCode = CaptchaCode, // Передаем проверенную капчу
                returnUrl = ReturnUrl
            });

        }
    }
}