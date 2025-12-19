using System;
using System.ComponentModel.DataAnnotations;

namespace Otzivi.Models
{
    public enum SecurityEventType
    {
        // Попытки входа
        FailedLoginAttempt = 1,          // Неудачная попытка входа
        MultipleFailedAttempts = 2,      // Много неудачных попыток (блокировка)
        SuccessfulLogin = 3,             // Успешный вход
        LoginFromNewDevice = 4,          // Вход с нового устройства/IP

        // Изменения аккаунта
        PasswordChanged = 10,            // Смена пароля
        EmailChanged = 11,               // Смена email
        TwoFactorEnabled = 12,           // Включение 2FA
        TwoFactorDisabled = 13,          // Выключение 2FA
        SecurityQuestionChanged = 14,    // Изменение секретного вопроса

        // Подозрительная активность
        SuspiciousActivity = 20,         // Подозрительная активность
        AccountLocked = 21,              // Блокировка аккаунта
        PasswordResetRequest = 22,       // Запрос сброса пароля

        // Административные события
        RoleChanged = 30,                // Изменение роли пользователя
        UserDeleted = 31,                // Удаление пользователя
        ContentModerated = 32            // Модерация контента
    }

    public enum SecurityEventSeverity
    {
        Low = 1,        // Информационные события
        Medium = 2,     // Предупреждения
        High = 3,       // Критические события
        Critical = 4    // Очень критичные
    }

    public class SecurityEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public SecurityEventType EventType { get; set; }

        [Required]
        public SecurityEventSeverity Severity { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string IpAddress { get; set; }

        [MaxLength(200)]
        public string UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsNotified { get; set; } = false;
        public DateTime? NotifiedAt { get; set; }

        // Навигационное свойство
        public virtual ApplicationUser User { get; set; }
    }
}