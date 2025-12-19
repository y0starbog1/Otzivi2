using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Otzivi.Models;
using Otzivi.Services;
using Otzivi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Otzivi.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly SimpleCaptchaService _captchaService;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly ISecurityQuestionService _securityQuestionService;
        private readonly ISecurityAlertService _securityAlertService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            SimpleCaptchaService captchaService,
            ILoginAttemptService loginAttemptService,
            ISecurityQuestionService securityQuestionService,
            ISecurityAlertService securityAlertService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _captchaService = captchaService;
            _loginAttemptService = loginAttemptService;
            _securityQuestionService = securityQuestionService;
            _securityAlertService = securityAlertService;
        }

        // 📌 РЕГИСТРАЦИЯ
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AccountRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!_captchaService.ValidateCaptcha(model.CaptchaCode))
            {
                ModelState.AddModelError("CaptchaCode", "Неверный код капчи");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = true,
                SecurityQuestionSetAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ РЕГИСТРАЦИИ
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                await _securityAlertService.RecordSecurityEventAsync(
                    user.Id,
                    SecurityEventType.SuccessfulLogin,
                    "Регистрация нового аккаунта",
                    ipAddress);

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Product");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // 📌 ВХОД
        [HttpGet]
        public IActionResult Login(string returnUrl = null, string timeout = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Проверяем параметр timeout
            if (!string.IsNullOrEmpty(timeout) && timeout.ToLower() == "true")
            {
                ViewData["TimeoutMessage"] = "Ваша сессия была завершена из-за неактивности.";
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AccountLoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            if (!_captchaService.ValidateCaptcha(model.CaptchaCode))
            {
                ModelState.AddModelError("CaptchaCode", "Неверный код капчи");
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            if (_loginAttemptService.IsBlocked(ipAddress))
            {
                var blockUntil = _loginAttemptService.GetBlockUntilTime(ipAddress);
                var timeLeft = blockUntil.HasValue ? (blockUntil.Value - DateTime.Now).Seconds : 0;
                ModelState.AddModelError(string.Empty, $"Слишком много попыток входа. Попробуйте через {timeLeft} секунд.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _loginAttemptService.RecordFailedAttempt(ipAddress);

                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ НЕУДАЧНОГО ВХОДА
                await _securityAlertService.RecordSecurityEventAsync(
                    "unknown",
                    SecurityEventType.FailedLoginAttempt,
                    $"Неудачная попытка входа с несуществующим email: {model.Email}",
                    ipAddress,
                    userAgent);

                ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
                return View(model);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _loginAttemptService.RecordSuccess(ipAddress);

                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ УСПЕШНОГО ВХОДА
                await _securityAlertService.RecordSecurityEventAsync(
                    user.Id,
                    SecurityEventType.SuccessfulLogin,
                    "Успешный вход в аккаунт",
                    ipAddress,
                    userAgent);

                // 🔐 ПРОВЕРЯЕМ ПОДОЗРИТЕЛЬНУЮ АКТИВНОСТЬ
                await _securityAlertService.CheckSuspiciousActivityAsync(user.Id, ipAddress);

                // 🔐 ПРОВЕРКА 2FA ЧЕРЕЗ СЕКРЕТНЫЙ ВОПРОС
                if (!string.IsNullOrEmpty(user.SecurityQuestion) && user.IsSecurityQuestionEnabled)
                {
                    // Сохраняем информацию для 2FA проверки
                    HttpContext.Session.SetString("2FA_UserId", user.Id);
                    HttpContext.Session.SetString("2FA_ReturnUrl", returnUrl);
                    HttpContext.Session.SetString("2FA_RememberMe", model.RememberMe.ToString());

                    return RedirectToAction("VerifySecurityQuestion", "Account");
                }

                await _signInManager.SignInAsync(user, model.RememberMe);

                return Url.IsLocalUrl(returnUrl)
                    ? (IActionResult)Redirect(returnUrl)
                    : RedirectToAction("Index", "Product");
            }
            else
            {
                _loginAttemptService.RecordFailedAttempt(ipAddress);

                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ НЕУДАЧНОГО ВХОДА
                await _securityAlertService.RecordSecurityEventAsync(
                    user.Id,
                    SecurityEventType.FailedLoginAttempt,
                    "Неудачная попытка входа: неверный пароль",
                    ipAddress,
                    userAgent);

                ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
                return View(model);
            }
        }

        // 📌 ПРОВЕРКА СЕКРЕТНОГО ВОПРОСА (2FA)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> VerifySecurityQuestion()
        {
            var userId = HttpContext.Session.GetString("2FA_UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.SecurityQuestion))
                return RedirectToAction("Login");

            var model = new VerifySecurityQuestionViewModel
            {
                UserId = userId,
                SecurityQuestion = user.SecurityQuestion,
                ReturnUrl = HttpContext.Session.GetString("2FA_ReturnUrl"),
                RememberMe = bool.Parse(HttpContext.Session.GetString("2FA_RememberMe") ?? "false")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifySecurityQuestion(VerifySecurityQuestionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return RedirectToAction("Login");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var isValid = await _securityQuestionService.VerifyUserSecurityAnswerAsync(user, model.SecurityAnswer);

            if (isValid)
            {
                // Очищаем сессию 2FA
                HttpContext.Session.Remove("2FA_UserId");
                HttpContext.Session.Remove("2FA_ReturnUrl");
                HttpContext.Session.Remove("2FA_RememberMe");

                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ УСПЕШНОЙ 2FA
                await _securityAlertService.RecordSecurityEventAsync(
                    user.Id,
                    SecurityEventType.SuccessfulLogin,
                    "Успешная проверка двухфакторной аутентификации (секретный вопрос)",
                    ipAddress,
                    userAgent);

                // Выполняем вход
                await _signInManager.SignInAsync(user, model.RememberMe);

                return Url.IsLocalUrl(model.ReturnUrl)
                    ? (IActionResult)Redirect(model.ReturnUrl)
                    : RedirectToAction("Index", "Product");
            }
            else
            {
                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ НЕУДАЧНОЙ 2FA
                await _securityAlertService.RecordSecurityEventAsync(
                    user.Id,
                    SecurityEventType.FailedLoginAttempt,
                    "Неверный ответ на секретный вопрос",
                    ipAddress,
                    userAgent);

                ModelState.AddModelError("SecurityAnswer", "Неверный ответ на секретный вопрос");
                return View(model);
            }
        }

        // 📌 ВЫХОД
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ ВЫХОДА
                await _securityAlertService.RecordSecurityEventAsync(
                    user.Id,
                    SecurityEventType.SuccessfulLogin, // Можно создать новый тип для выхода
                    "Выход из аккаунта",
                    ipAddress);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Product");
        }

        // 📌 ЛИЧНЫЙ КАБИНЕТ
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            ViewBag.Email = user.Email;
            ViewBag.UserId = user.Id;
            ViewBag.FirstName = user.FirstName;
            ViewBag.LastName = user.LastName;

            // 🔐 ИНФОРМАЦИЯ О СЕКРЕТНОМ ВОПРОСЕ
            ViewBag.SecurityQuestion = !string.IsNullOrEmpty(user.SecurityQuestion)
                ? user.SecurityQuestion
                : "Секретный вопрос не установлен";

            ViewBag.IsSecurityQuestionEnabled = user.IsSecurityQuestionEnabled;
            ViewBag.HasSecurityQuestion = !string.IsNullOrEmpty(user.SecurityQuestion);

            return View();
        }

        // 📌 УСТАНОВКА СЕКРЕТНОГО ВОПРОСА
        [HttpGet]
        [Authorize]
        public IActionResult SetSecurityQuestion()
        {
            var model = new SetSecurityQuestionViewModel
            {
                AvailableQuestions = new List<string>
                {
                    "Как звали вашего первого питомца?",
                    "Девичья фамилия вашей матери?",
                    "В каком городе вы родились?",
                    "Как звали вашу первую учительницу?",
                    "Какое прозвище было у вас в детстве?"
                }
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetSecurityQuestion(SetSecurityQuestionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableQuestions = new List<string>
                {
                    "Как звали вашего первого питомца?",
                    "Девичья фамилия вашей матери?",
                    "В каком городе вы родились?",
                    "Как звали вашу первую учительницу?",
                    "Какое прозвище было у вас в детстве?"
                };
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var result = await _securityQuestionService.SetSecurityQuestionAsync(user, model.SecurityQuestion, model.SecurityAnswer);

            if (result)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                if (model.Enable2FA)
                {
                    user.IsSecurityQuestionEnabled = true;
                    await _userManager.UpdateAsync(user);

                    // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ ВКЛЮЧЕНИЯ 2FA
                    await _securityAlertService.RecordSecurityEventAsync(
                        user.Id,
                        SecurityEventType.TwoFactorEnabled,
                        "Включение двухфакторной аутентификации (секретный вопрос)",
                        ipAddress);
                }

                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ ИЗМЕНЕНИЯ СЕКРЕТНОГО ВОПРОСА
                await _securityAlertService.RecordSecurityEventAsync(
                    user.Id,
                    SecurityEventType.SecurityQuestionChanged,
                    "Установка/изменение секретного вопроса",
                    ipAddress);

                TempData["StatusMessage"] = "Секретный вопрос успешно установлен!";
                return RedirectToAction("Dashboard");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Ошибка при установке секретного вопроса");
                return View(model);
            }
        }

        // 📌 ВКЛЮЧЕНИЕ/ВЫКЛЮЧЕНИЕ 2FA
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSecurityQuestion2FA(bool enable)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (string.IsNullOrEmpty(user.SecurityQuestion))
            {
                TempData["ErrorMessage"] = "Сначала установите секретный вопрос!";
                return RedirectToAction("Dashboard");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            user.IsSecurityQuestionEnabled = enable;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // 🔐 ЗАПИСЫВАЕМ СОБЫТИЕ ИЗМЕНЕНИЯ 2FA
                var eventType = enable ? SecurityEventType.TwoFactorEnabled : SecurityEventType.TwoFactorDisabled;
                var description = enable ? "Включение 2FA" : "Выключение 2FA";

                await _securityAlertService.RecordSecurityEventAsync(
                    user.Id,
                    eventType,
                    description,
                    ipAddress);

                TempData["StatusMessage"] = enable
                    ? "Двухфакторная аутентификация включена!"
                    : "Двухфакторная аутентификация выключена!";
            }
            else
            {
                TempData["ErrorMessage"] = "Ошибка при изменении настроек 2FA";
            }

            return RedirectToAction("Dashboard");
        }

        // 📌 ДОСТУП ЗАПРЕЩЕН
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}