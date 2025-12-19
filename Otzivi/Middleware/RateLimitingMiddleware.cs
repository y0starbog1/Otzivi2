using Otzivi.Services;

namespace Otzivi.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ILoginAttemptService loginAttemptService)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Логируем ВСЕ запросы к логину для отладки
            if (context.Request.Path.StartsWithSegments("/Identity/Account/Login"))
            {
                _logger.LogInformation($"🔐 Login attempt from IP: {ipAddress}, Method: {context.Request.Method}");
            }

            // Проверяем только POST запросы к странице входа
            if (context.Request.Path.StartsWithSegments("/Identity/Account/Login") &&
                context.Request.Method == "POST")
            {
                if (loginAttemptService.IsBlocked(ipAddress))
                {
                    _logger.LogWarning($"🚫 BLOCKED: IP {ipAddress} заблокирован за слишком много попыток входа");
                    context.Response.StatusCode = 429;
                    context.Response.Redirect("/Identity/Account/Lockout");
                    return;
                }
            }

            await _next(context);
        }
    }
}