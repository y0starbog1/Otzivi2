using Microsoft.AspNetCore.Identity;
using Otzivi.Models;
using BCrypt.Net;

namespace Otzivi.Services
{
    public class BCryptPasswordHasher : IPasswordHasher<ApplicationUser>
    {
        private readonly PasswordHasher<ApplicationUser> _identityHasher;

        public BCryptPasswordHasher()
        {
            _identityHasher = new PasswordHasher<ApplicationUser>();
        }

        public string HashPassword(ApplicationUser user, string password)
        {
            // Всегда используем BCrypt для новых паролей
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashType.SHA384);
        }

        public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
        {
            // Сначала проверяем, это BCrypt хэш?
            if (IsBCryptHash(hashedPassword))
            {
                // Проверяем через BCrypt
                var isValid = BCrypt.Net.BCrypt.EnhancedVerify(providedPassword, hashedPassword, HashType.SHA384);
                return isValid ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
            }
            else
            {
                // Это старый Identity хэш - проверяем через стандартный хэшер
                var result = _identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

                // Если пароль верный, перехешируем его в BCrypt
                if (result == PasswordVerificationResult.Success)
                {
                    // Здесь можно обновить хэш в базе данных (опционально)
                    // await UpdatePasswordHash(user, providedPassword);
                }

                return result;
            }
        }

        private bool IsBCryptHash(string hash)
        {
            // BCrypt хэши начинаются с $2a$, $2b$, $2x$, $2y$
            return !string.IsNullOrEmpty(hash) &&
                   hash.Length >= 4 &&
                   hash.StartsWith("$2");
        }

        // Опционально: метод для обновления хэша при следующем входе
        private async Task UpdatePasswordHash(ApplicationUser user, string newPassword)
        {
            // Этот метод можно вызвать при следующем успешном входе
            // чтобы перевести пользователя на BCrypt
        }
    }
}