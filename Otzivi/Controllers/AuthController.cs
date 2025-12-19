// Controllers/AuthController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Otzivi.Models;
using Otzivi.Services;

namespace Otzivi.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ISecurityQuestionService _securityQuestionService;
        private readonly SimpleCaptchaService _captchaService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ISecurityQuestionService securityQuestionService,
            SimpleCaptchaService captchaService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _securityQuestionService = securityQuestionService;
            _captchaService = captchaService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Просто показываем форму, капча будет загружена через JavaScript
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            string captchaCode, // Добавляем параметр для капчи
            bool rememberMe = false,
            string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            Console.WriteLine($"=== LOGIN ATTEMPT ===");
            Console.WriteLine($"Email: {email}");
            Console.WriteLine($"Captcha Code: {captchaCode}");
            Console.WriteLine($"Session ID: {HttpContext.Session?.Id}");
            Console.WriteLine($"Session IsAvailable: {HttpContext.Session?.IsAvailable}");

            // 1. Проверяем капчу ПЕРЕД проверкой пользователя
            var captchaValid = _captchaService.ValidateCaptcha(captchaCode);
            Console.WriteLine($"Captcha validation result: {captchaValid}");

            if (!captchaValid)
            {
                ViewBag.Error = "Неверный код с картинки";
                Console.WriteLine("Капча не прошла проверку!");
                return View();
            }

            Console.WriteLine("Капча прошла проверку!");

            // 2. Простая валидация
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Введите email и пароль";
                return View();
            }

            // 3. Ищем пользователя по email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Неверный email или пароль";
                return View();
            }

            // 4. Проверяем пароль
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // 🔐 ПРОВЕРЯЕМ ВКЛЮЧЕНА ЛИ 2FA
                if (user.IsSecurityQuestionEnabled && !string.IsNullOrEmpty(user.SecurityQuestion))
                {
                    // Сохраняем данные для проверки секретного вопроса
                    TempData["TwoFactorUserId"] = user.Id;
                    TempData["ReturnUrl"] = returnUrl;
                    TempData["RememberMe"] = rememberMe;

                    return RedirectToAction("Verify", "SecurityQuestion");
                }

                // ЕСЛИ 2FA НЕ ВКЛЮЧЕНА - ВХОДИМ СРАЗУ
                await _signInManager.SignInAsync(user, rememberMe);
                return LocalRedirect(returnUrl);
            }
            else
            {
                ViewBag.Error = "Неверный email или пароль";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Product");
        }
    }
}