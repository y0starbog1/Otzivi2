using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Otzivi.Middleware
{
    public class SessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionTimeoutMiddleware> _logger;
        private readonly TimeSpan _timeout;

        public SessionTimeoutMiddleware(
            RequestDelegate next,
            ILogger<SessionTimeoutMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _timeout = TimeSpan.FromMinutes(1); // 1 минута для теста
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Пропускаем статические файлы и некоторые пути
            if (ShouldSkip(context))
            {
                await _next(context);
                return;
            }

            // Проверяем аутентифицированных пользователей
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var session = context.Session;

                // Получаем время последней активности
                var lastActivityStr = session.GetString("LastActivity");
                var now = DateTime.Now;

                if (string.IsNullOrEmpty(lastActivityStr))
                {
                    // Первая активность
                    session.SetString("LastActivity", now.ToString("o"));
                    _logger.LogInformation($"🆕 Session started for {context.User.Identity.Name}");
                }
                else if (DateTime.TryParse(lastActivityStr, out var lastActivity))
                {
                    var idleTime = now - lastActivity;

                    _logger.LogDebug($"⏰ User {context.User.Identity.Name}: {idleTime.TotalSeconds:F0}s idle");

                    // Проверяем таймаут
                    if (idleTime > _timeout)
                    {
                        _logger.LogWarning($"⏰ SESSION TIMEOUT for {context.User.Identity.Name} - {idleTime.TotalMinutes:F1} minutes idle");

                        // Очищаем аутентификацию
                        await context.SignOutAsync();
                        session.Clear();

                        // ПРОСТОЙ редирект без параметров для избежания ошибок
                        context.Response.Redirect("/Account/Login?timeout=true");
                        return;
                    }
                }

                // Обновляем время активности
                session.SetString("LastActivity", now.ToString("o"));
            }

            await _next(context);
        }

        private bool ShouldSkip(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // Пропускаем пути связанные с аутентификацией и статические файлы
            if (path.StartsWith("/Account/Login") ||
                path.StartsWith("/Account/Register") ||
                path.StartsWith("/Account/Logout") ||
                path.StartsWith("/Captcha/") ||
                path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/lib/") ||
                path.StartsWith("/images/") ||
                path.StartsWith("/_") ||
                path == "/favicon.ico")
            {
                return true;
            }

            return false;
        }
    }
}