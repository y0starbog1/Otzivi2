using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Otzivi.Models;
using Otzivi.Services;

namespace Otzivi.Views.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoginAttemptService _loginAttemptService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILoginAttemptService loginAttemptService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _loginAttemptService = loginAttemptService;
        }

        public async Task<IActionResult> OnPostAsync(string email, string password, bool rememberMe, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // 🔐 RATE-LIMITING ПРОВЕРКА
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            Console.WriteLine($"🔐 LOGIN ATTEMPT: {email} from IP: {ipAddress}");

            if (_loginAttemptService.IsBlocked(ipAddress))
            {
                var blockUntil = _loginAttemptService.GetBlockUntilTime(ipAddress);
                var timeLeft = blockUntil.HasValue ? (blockUntil.Value - DateTime.Now).Seconds : 0;

                Console.WriteLine($"🚫 BLOCKED: IP {ipAddress} заблокирован на {timeLeft}сек");
                ModelState.AddModelError(string.Empty, $"Слишком много попыток входа. Попробуйте через {timeLeft} секунд.");
                return Page();
            }

            // Ищем пользователя
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _loginAttemptService.RecordFailedAttempt(ipAddress);
                var remaining = _loginAttemptService.GetRemainingAttempts(ipAddress);
                Console.WriteLine($"❌ USER NOT FOUND: {email}, Remaining: {remaining}");
                ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
                return Page();
            }

            // Проверяем пароль
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                Console.WriteLine($"✅ LOGIN SUCCESS: {user.UserName}");
                _loginAttemptService.RecordSuccess(ipAddress);
                await _signInManager.SignInAsync(user, rememberMe);
                return LocalRedirect(returnUrl);
            }
            else
            {
                _loginAttemptService.RecordFailedAttempt(ipAddress);
                var remaining = _loginAttemptService.GetRemainingAttempts(ipAddress);
                Console.WriteLine($"❌ LOGIN FAILED: {email}, Remaining: {remaining}");
                ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
                return Page();
            }
        }
    }
}