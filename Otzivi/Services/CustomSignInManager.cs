using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Otzivi.Models;
using Otzivi.Services;

namespace Otzivi.Services
{
    public class CustomSignInManager : SignInManager<ApplicationUser>
    {
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly IHttpContextAccessor _contextAccessor;

        public CustomSignInManager(UserManager<ApplicationUser> userManager,
                                 IHttpContextAccessor contextAccessor,
                                 IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
                                 Microsoft.Extensions.Options.IOptions<IdentityOptions> optionsAccessor,
                                 ILogger<SignInManager<ApplicationUser>> logger,
                                 IAuthenticationSchemeProvider schemes,
                                 IUserConfirmation<ApplicationUser> confirmation,
                                 ILoginAttemptService loginAttemptService)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _loginAttemptService = loginAttemptService;
            _contextAccessor = contextAccessor;
        }

        public override async Task<SignInResult> PasswordSignInAsync(string userName, string password,
    bool isPersistent, bool lockoutOnFailure)
        {
            var ipAddress = _contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // 🔐 RATE-LIMITING ПРОВЕРКА
            if (_loginAttemptService.IsBlocked(ipAddress))
            {
                Logger.LogWarning($"🚫 BLOCKED: IP {ipAddress} пытается войти как {userName}");
                return SignInResult.Failed;
            }

            // Находим пользователя для проверки 2FA
            var user = await UserManager.FindByNameAsync(userName);
            if (user == null)
            {
                // Пользователь не найден - все равно записываем попытку
                _loginAttemptService.RecordFailedAttempt(ipAddress);
                return SignInResult.Failed;
            }

            // 🔐 ПРОВЕРЯЕМ ВКЛЮЧЕНА ЛИ 2FA
            if (user.IsSecurityQuestionEnabled && !string.IsNullOrEmpty(user.SecurityQuestion))
            {
                // Проверяем пароль отдельно (не входим сразу)
                var passwordValid = await UserManager.CheckPasswordAsync(user, password);
                if (passwordValid)
                {
                    // Пароль верный, но нужна 2FA - записываем успех для rate-limiting
                    _loginAttemptService.RecordSuccess(ipAddress);

                    // Сохраняем данные для 2FA
                    var context = _contextAccessor.HttpContext;
                    context.Items["2FA_UserId"] = user.Id;
                    context.Items["2FA_ReturnUrl"] = context.Request.Query["ReturnUrl"].ToString();
                    context.Items["2FA_RememberMe"] = isPersistent;

                    Logger.LogInformation($"🔐 2FA REQUIRED for {userName}");
                    return SignInResult.TwoFactorRequired;
                }
                else
                {
                    _loginAttemptService.RecordFailedAttempt(ipAddress);
                    var remaining = _loginAttemptService.GetRemainingAttempts(ipAddress);
                    Logger.LogWarning($"❌ Failed login for {userName} from {ipAddress}. Remaining: {remaining}");
                    return SignInResult.Failed;
                }
            }

            // Обычный вход без 2FA
            var result = await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);

            if (!result.Succeeded)
            {
                _loginAttemptService.RecordFailedAttempt(ipAddress);
                var remaining = _loginAttemptService.GetRemainingAttempts(ipAddress);
                Logger.LogWarning($"❌ Failed login for {userName} from {ipAddress}. Remaining: {remaining}");
            }
            else
            {
                _loginAttemptService.RecordSuccess(ipAddress);
                Logger.LogInformation($"✅ Successful login for {userName} from {ipAddress}");
            }

            return result;
        }
    }
}