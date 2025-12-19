using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Otzivi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔐 ПОЛЯ ДЛЯ СЕКРЕТНОГО ВОПРОСА 2FA
        public string? SecurityQuestion { get; set; }
        public string? SecurityAnswerHash { get; set; }
        public bool IsSecurityQuestionEnabled { get; set; } = false;
        public DateTime? SecurityQuestionSetAt { get; set; }

        // Навигационные свойства
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<ReviewLike> ReviewLikes { get; set; } = new List<ReviewLike>();

        // 🔐 ДОБАВЬТЕ ЭТУ СТРОКУ - коллекция событий безопасности
        public virtual ICollection<SecurityEvent> SecurityEvents { get; set; } = new List<SecurityEvent>();
    }
}