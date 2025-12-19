using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Otzivi.Data;
using Otzivi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otzivi.Services
{
    public class SecurityAlertService : ISecurityAlertService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SecurityAlertService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecurityAlertService(
            ApplicationDbContext context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            ILogger<SecurityAlertService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RecordSecurityEventAsync(
            string userId,
            SecurityEventType eventType,
            string description,
            string ipAddress = null,
            string userAgent = null)
        {
            try
            {
                // Если IP не указан, пытаемся получить из контекста
                if (string.IsNullOrEmpty(ipAddress) && _httpContextAccessor.HttpContext != null)
                {
                    ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
                }

                // Если UserAgent не указан, пытаемся получить из запроса
                if (string.IsNullOrEmpty(userAgent) && _httpContextAccessor.HttpContext != null)
                {
                    userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"];
                }

                var securityEvent = new SecurityEvent
                {
                    UserId = userId,
                    EventType = eventType,
                    Severity = GetSeverityByEventType(eventType),
                    Description = description,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SecurityEvents.Add(securityEvent);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"📊 Security event recorded: {eventType} for user {userId}");

                // Отправляем оповещение если это критическое событие
                if (securityEvent.Severity >= SecurityEventSeverity.Medium)
                {
                    await SendSecurityAlertAsync(securityEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error recording security event: {ex.Message}");
            }
        }

        public async Task SendSecurityAlertAsync(SecurityEvent securityEvent)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(securityEvent.UserId);
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning($"Cannot send alert: user {securityEvent.UserId} not found");
                    return;
                }

                string subject = GetEmailSubject(securityEvent.EventType, securityEvent.Severity);
                string message = GetEmailMessage(securityEvent, user);

                await _emailSender.SendEmailAsync(user.Email, subject, message);

                // Отправляем также админу если событие критическое
                if (securityEvent.Severity >= SecurityEventSeverity.High)
                {
                    await SendAdminAlertAsync(securityEvent, user);
                }

                // Отмечаем как отправленное
                securityEvent.IsNotified = true;
                securityEvent.NotifiedAt = DateTime.UtcNow;
                _context.SecurityEvents.Update(securityEvent);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"📧 Security alert sent to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending security alert: {ex.Message}");
            }
        }

        public async Task<bool> CheckSuspiciousActivityAsync(string userId, string ipAddress)
        {
            // Проверяем количество событий за последний час
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            var recentEvents = await _context.SecurityEvents
                .Where(e => e.UserId == userId && e.CreatedAt > oneHourAgo)
                .CountAsync();

            // Если больше 10 событий за час - подозрительно
            if (recentEvents > 10)
            {
                await RecordSecurityEventAsync(
                    userId,
                    SecurityEventType.SuspiciousActivity,
                    $"Подозрительная активность: {recentEvents} событий за последний час",
                    ipAddress);
                return true;
            }

            return false;
        }

        public async Task<List<SecurityEvent>> GetUserSecurityEventsAsync(string userId, int limit = 50)
        {
            return await _context.SecurityEvents
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<SecurityEvent>> GetAllSecurityEventsAsync(int limit = 100)
        {
            return await _context.SecurityEvents
                .Include(e => e.User)
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        #region Helper Methods

        private SecurityEventSeverity GetSeverityByEventType(SecurityEventType eventType)
        {
            return eventType switch
            {
                // Критические события
                SecurityEventType.MultipleFailedAttempts => SecurityEventSeverity.Critical,
                SecurityEventType.AccountLocked => SecurityEventSeverity.Critical,

                // Высокая важность
                SecurityEventType.PasswordChanged => SecurityEventSeverity.High,
                SecurityEventType.EmailChanged => SecurityEventSeverity.High,
                SecurityEventType.TwoFactorDisabled => SecurityEventSeverity.High,
                SecurityEventType.UserDeleted => SecurityEventSeverity.High,

                // Средняя важность
                SecurityEventType.FailedLoginAttempt => SecurityEventSeverity.Medium,
                SecurityEventType.LoginFromNewDevice => SecurityEventSeverity.Medium,
                SecurityEventType.SecurityQuestionChanged => SecurityEventSeverity.Medium,
                SecurityEventType.PasswordResetRequest => SecurityEventSeverity.Medium,
                SecurityEventType.SuspiciousActivity => SecurityEventSeverity.Medium,
                SecurityEventType.RoleChanged => SecurityEventSeverity.Medium,

                // Низкая важность
                SecurityEventType.SuccessfulLogin => SecurityEventSeverity.Low,
                SecurityEventType.TwoFactorEnabled => SecurityEventSeverity.Low,
                SecurityEventType.ContentModerated => SecurityEventSeverity.Low,

                _ => SecurityEventSeverity.Low
            };
        }

        private string GetEmailSubject(SecurityEventType eventType, SecurityEventSeverity severity)
        {
            var severityIcon = severity switch
            {
                SecurityEventSeverity.Critical => "🚨",
                SecurityEventSeverity.High => "⚠️",
                SecurityEventSeverity.Medium => "🔔",
                _ => "ℹ️"
            };

            var eventName = eventType switch
            {
                SecurityEventType.FailedLoginAttempt => "Неудачная попытка входа",
                SecurityEventType.MultipleFailedAttempts => "Многократные неудачные попытки входа",
                SecurityEventType.SuccessfulLogin => "Успешный вход в аккаунт",
                SecurityEventType.LoginFromNewDevice => "Вход с нового устройства",
                SecurityEventType.PasswordChanged => "Смена пароля",
                SecurityEventType.EmailChanged => "Смена email",
                SecurityEventType.TwoFactorEnabled => "Включение двухфакторной аутентификации",
                SecurityEventType.TwoFactorDisabled => "Выключение двухфакторной аутентификации",
                SecurityEventType.SecurityQuestionChanged => "Изменение секретного вопроса",
                SecurityEventType.SuspiciousActivity => "Подозрительная активность",
                SecurityEventType.AccountLocked => "Блокировка аккаунта",
                SecurityEventType.PasswordResetRequest => "Запрос сброса пароля",
                SecurityEventType.RoleChanged => "Изменение роли пользователя",
                SecurityEventType.UserDeleted => "Удаление пользователя",
                SecurityEventType.ContentModerated => "Модерация контента",
                _ => "Событие безопасности"
            };

            return $"{severityIcon} Otzivi: {eventName}";
        }

        private string GetEmailMessage(SecurityEvent securityEvent, ApplicationUser user)
        {
            var time = securityEvent.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            var ipInfo = !string.IsNullOrEmpty(securityEvent.IpAddress)
                ? $"<p><strong>IP-адрес:</strong> {securityEvent.IpAddress}</p>"
                : "";

            var baseMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #333;'>{GetEmailSubject(securityEvent.EventType, securityEvent.Severity)}</h2>
                    
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Пользователь:</strong> {user.UserName}</p>
                        <p><strong>Время события:</strong> {time}</p>
                        {ipInfo}
                        <p><strong>Описание:</strong> {securityEvent.Description}</p>
                    </div>
                    
                    <div style='margin-top: 30px; padding: 15px; background: #e9ecef; border-radius: 5px;'>
                        <h3 style='color: #666;'>Рекомендации:</h3>
                        {GetRecommendations(securityEvent.EventType)}
                    </div>
                    
                    <div style='margin-top: 20px; font-size: 12px; color: #666;'>
                        <p>Это автоматическое сообщение от системы безопасности Otzivi.</p>
                        <p>Если вы не совершали это действие, пожалуйста, немедленно свяжитесь с поддержкой.</p>
                    </div>
                </div>
            ";

            return baseMessage;
        }

        private string GetRecommendations(SecurityEventType eventType)
        {
            return eventType switch
            {
                SecurityEventType.FailedLoginAttempt =>
                    "<p>• Если это были не вы, проверьте надежность вашего пароля</p>" +
                    "<p>• Рекомендуем включить двухфакторную аутентификацию</p>",

                SecurityEventType.MultipleFailedAttempts =>
                    "<p>• Ваш аккаунт был временно заблокирован</p>" +
                    "<p>• Проверьте, не был ли скомпрометирован ваш пароль</p>" +
                    "<p>• Свяжитесь с поддержкой если подозреваете взлом</p>",

                SecurityEventType.LoginFromNewDevice =>
                    "<p>• Убедитесь, что это были вы</p>" +
                    "<p>• Проверьте список активных сессий в настройках аккаунта</p>",

                SecurityEventType.PasswordChanged =>
                    "<p>• Если это были не вы, немедленно восстановите доступ через 'Забыли пароль?'</p>" +
                    "<p>• Проверьте настройки безопасности вашего аккаунта</p>",

                SecurityEventType.TwoFactorDisabled =>
                    "<p>• Если это были не вы, немедленно включите двухфакторную аутентификацию снова</p>" +
                    "<p>• Смените пароль для безопасности</p>",

                _ => "<p>• Убедитесь, что все действия в вашем аккаунте были совершены вами</p>" +
                     "<p>• Регулярно проверяйте настройки безопасности</p>"
            };
        }

        private async Task SendAdminAlertAsync(SecurityEvent securityEvent, ApplicationUser user)
        {
            try
            {
                // Получаем всех администраторов
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                foreach (var admin in admins)
                {
                    if (!string.IsNullOrEmpty(admin.Email))
                    {
                        var adminSubject = $"🚨 КРИТИЧЕСКОЕ СОБЫТИЕ: {securityEvent.EventType}";
                        var adminMessage = $@"
                            <h2>Критическое событие безопасности</h2>
                            <p><strong>Пользователь:</strong> {user.UserName} ({user.Email})</p>
                            <p><strong>Событие:</strong> {securityEvent.EventType}</p>
                            <p><strong>Время:</strong> {securityEvent.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}</p>
                            <p><strong>IP:</strong> {securityEvent.IpAddress}</p>
                            <p><strong>Описание:</strong> {securityEvent.Description}</p>
                            <p><strong>User-Agent:</strong> {securityEvent.UserAgent}</p>
                        ";

                        await _emailSender.SendEmailAsync(admin.Email, adminSubject, adminMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin alert");
            }
        }

        #endregion
    }
}