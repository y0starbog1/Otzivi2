using Microsoft.Extensions.Caching.Memory;
using Otzivi.Models;
using Microsoft.Extensions.Logging;

namespace Otzivi.Services
{
    public interface ILoginAttemptService
    {
        bool IsBlocked(string ipAddress);
        void RecordFailedAttempt(string ipAddress);
        void RecordSuccess(string ipAddress);
        int GetRemainingAttempts(string ipAddress);
        DateTime? GetBlockUntilTime(string ipAddress);
    }

    public class LoginAttemptService : ILoginAttemptService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<LoginAttemptService> _logger;
        private readonly ISecurityAlertService _securityAlertService;
        private const int MAX_ATTEMPTS = 5;
        private readonly TimeSpan BLOCK_TIME = TimeSpan.FromSeconds(30); // 👈 30 СЕКУНД

        public LoginAttemptService(
            IMemoryCache cache,
            ILogger<LoginAttemptService> logger,
            ISecurityAlertService securityAlertService) // 👈 ДОБАВЛЕН НОВЫЙ ПАРАМЕТР
        {
            _cache = cache;
            _logger = logger;
            _securityAlertService = securityAlertService;
        }

        public bool IsBlocked(string ipAddress)
        {
            var key = $"login_attempt_{ipAddress}";
            if (_cache.TryGetValue<LoginAttempt>(key, out var attempt))
            {
                var isBlocked = attempt.IsCurrentlyBlocked;
                if (isBlocked)
                {
                    var timeLeft = attempt.BlockUntil - DateTime.Now;
                    _logger.LogWarning($"🚫 IP {ipAddress} заблокирован. Разблокировка через: {timeLeft.Seconds}сек");
                }
                return isBlocked;
            }
            return false;
        }

        public void RecordFailedAttempt(string ipAddress)
        {
            var key = $"login_attempt_{ipAddress}";
            if (!_cache.TryGetValue<LoginAttempt>(key, out var attempt))
            {
                attempt = new LoginAttempt
                {
                    IpAddress = ipAddress,
                    FirstAttempt = DateTime.Now,
                    AttemptCount = 0
                };
            }

            attempt.AttemptCount++;
            attempt.LastAttempt = DateTime.Now;

            _cache.Set(key, attempt, TimeSpan.FromHours(1));

            _logger.LogInformation($"Записана неудачная попытка входа для IP {ipAddress}. Попытка: {attempt.AttemptCount}/5");

            // 🔐 ЕСЛИ ДОСТИГЛИ ЛИМИТА - ОТПРАВЛЯЕМ КРИТИЧЕСКОЕ ОПОВЕЩЕНИЕ
            if (attempt.AttemptCount >= MAX_ATTEMPTS)
            {
                // Отправляем событие о блокировке в фоновом режиме
                Task.Run(async () =>
                {
                    try
                    {
                        await _securityAlertService.RecordSecurityEventAsync(
                            "system", // системное событие
                            SecurityEventType.MultipleFailedAttempts,
                            $"IP {ipAddress} заблокирован после {attempt.AttemptCount} неудачных попыток входа",
                            ipAddress);

                        _logger.LogWarning($"🚨 CRITICAL: IP {ipAddress} заблокирован! Отправлено оповещение.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Ошибка при отправке оповещения о блокировке IP {ipAddress}");
                    }
                });
            }
            // 🔐 ЕСЛИ БОЛЬШЕ 3 ПОПЫТОК - ОТПРАВЛЯЕМ ПРЕДУПРЕЖДЕНИЕ
            else if (attempt.AttemptCount >= 3)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _securityAlertService.RecordSecurityEventAsync(
                            "system",
                            SecurityEventType.SuspiciousActivity,
                            $"Подозрительная активность: {attempt.AttemptCount} неудачных попыток входа с IP {ipAddress}",
                            ipAddress);
                    }
                    catch { /* Игнорируем ошибки в фоновой задаче */ }
                });
            }
        }

        public void RecordSuccess(string ipAddress)
        {
            var key = $"login_attempt_{ipAddress}";
            if (_cache.TryGetValue<LoginAttempt>(key, out var attempt))
            {
                _logger.LogInformation($"✅ Сброс счетчика для IP {ipAddress}. Было попыток: {attempt.AttemptCount}");

                // 🔐 ЕСЛИ БЫЛИ НЕУДАЧНЫЕ ПОПЫТКИ - ЗАПИСЫВАЕМ СОБЫТИЕ
                if (attempt.AttemptCount > 0)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _securityAlertService.RecordSecurityEventAsync(
                                "system",
                                SecurityEventType.SuccessfulLogin,
                                $"Успешный вход после {attempt.AttemptCount} неудачных попыток с IP {ipAddress}",
                                ipAddress);
                        }
                        catch { /* Игнорируем */ }
                    });
                }
            }
            _cache.Remove(key);
        }

        public int GetRemainingAttempts(string ipAddress)
        {
            var key = $"login_attempt_{ipAddress}";
            if (_cache.TryGetValue<LoginAttempt>(key, out var attempt))
            {
                return Math.Max(0, MAX_ATTEMPTS - attempt.AttemptCount);
            }
            return MAX_ATTEMPTS;
        }

        public DateTime? GetBlockUntilTime(string ipAddress)
        {
            var key = $"login_attempt_{ipAddress}";
            if (_cache.TryGetValue<LoginAttempt>(key, out var attempt) && attempt.IsCurrentlyBlocked)
            {
                return attempt.BlockUntil;
            }
            return null;
        }
    }
}