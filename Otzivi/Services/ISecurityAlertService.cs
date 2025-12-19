using Otzivi.Models;
using System.Threading.Tasks;

namespace Otzivi.Services
{
    public interface ISecurityAlertService
    {
        // Регистрация событий безопасности
        Task RecordSecurityEventAsync(
            string userId,
            SecurityEventType eventType,
            string description,
            string ipAddress = null,
            string userAgent = null);

        // Отправка оповещений
        Task SendSecurityAlertAsync(SecurityEvent securityEvent);

        // Проверка подозрительной активности
        Task<bool> CheckSuspiciousActivityAsync(string userId, string ipAddress);

        // Получение истории событий
        Task<List<SecurityEvent>> GetUserSecurityEventsAsync(string userId, int limit = 50);

        // Получение всех событий (для админа)
        Task<List<SecurityEvent>> GetAllSecurityEventsAsync(int limit = 100);
    }
}