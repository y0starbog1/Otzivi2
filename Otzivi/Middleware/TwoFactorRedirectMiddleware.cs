using Otzivi.Services;

namespace Otzivi.Middleware
{
    public class TwoFactorRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public TwoFactorRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Перехватываем случаи когда требуется 2FA
            if (context.Response.StatusCode == 302 && context.Items.ContainsKey("2FA_UserId"))
            {
                var userId = context.Items["2FA_UserId"] as string;
                var returnUrl = context.Items["2FA_ReturnUrl"] as string ?? "/";
                var rememberMe = context.Items["2FA_RememberMe"] as bool? ?? false;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Перенаправляем на страницу секретного вопроса
                    context.Response.Redirect($"/SecurityQuestion/Verify?userId={userId}&returnUrl={returnUrl}&rememberMe={rememberMe}");
                    return;
                }
            }
        }
    }
}