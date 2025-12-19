using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Otzivi.Models;
using Otzivi.Services;

namespace Otzivi.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly ISecurityQuestionService _securityQuestionService;

        public LoginController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILoginAttemptService loginAttemptService,
            ISecurityQuestionService securityQuestionService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _loginAttemptService = loginAttemptService;
            _securityQuestionService = securityQuestionService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            bool rememberMe = false,
            string captchaCode = null, // Добавляем параметр для капчи (уже проверена)
            string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Проверяем блокировку по IP
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (_loginAttemptService.IsBlocked(ipAddress))
            {
                TempData["ErrorMessage"] = "Слишком много попыток входа. Попробуйте позже.";
                return RedirectToPage("/Account/Login");
            }

            // Капча уже проверена на странице, но для безопасности можно проверить еще раз
            // (если передали captchaCode)

            // Ищем пользователя по email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _loginAttemptService.RecordFailedAttempt(ipAddress);
                TempData["ErrorMessage"] = "Неверный email или пароль.";
                return RedirectToPage("/Account/Login");
            }

            // Проверяем пароль
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // 🔐 ЕСЛИ ВКЛЮЧЕНА 2FA ЧЕРЕЗ СЕКРЕТНЫЙ ВОПРОС
                if (user.IsSecurityQuestionEnabled && !string.IsNullOrEmpty(user.SecurityQuestion))
                {
                    // Перенаправляем на нашу страницу проверки секретного вопроса
                    return RedirectToAction("Verify", "SecurityQuestion", new
                    {
                        userId = user.Id,
                        returnUrl = returnUrl
                    });
                }

                // ЕСЛИ 2FA НЕ ВКЛЮЧЕНА - ВХОДИМ СРАЗУ
                await _signInManager.SignInAsync(user, rememberMe);
                _loginAttemptService.RecordSuccess(ipAddress);
                return LocalRedirect(returnUrl);
            }
            else
            {
                // Неудачная попытка входа
                _loginAttemptService.RecordFailedAttempt(ipAddress);
                TempData["ErrorMessage"] = "Неверный email или пароль.";
                return RedirectToPage("/Account/Login");
            }
        }
    }
}