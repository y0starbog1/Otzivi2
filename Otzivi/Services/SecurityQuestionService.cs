using Microsoft.AspNetCore.Identity;
using Otzivi.Models;
using BCrypt.Net;

namespace Otzivi.Services
{
    public interface ISecurityQuestionService
    {
        string HashSecurityAnswer(string answer);
        bool VerifySecurityAnswer(string answer, string hashedAnswer);
        Task<bool> SetSecurityQuestionAsync(ApplicationUser user, string question, string answer);
        Task<bool> VerifyUserSecurityAnswerAsync(ApplicationUser user, string answer);
    }

    public class SecurityQuestionService : ISecurityQuestionService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public SecurityQuestionService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public string HashSecurityAnswer(string answer)
        {
            // Хэшируем ответ как пароль
            return BCrypt.Net.BCrypt.EnhancedHashPassword(answer.Trim().ToLower(), HashType.SHA384);
        }

        public bool VerifySecurityAnswer(string answer, string hashedAnswer)
        {
            if (string.IsNullOrEmpty(answer) || string.IsNullOrEmpty(hashedAnswer))
                return false;

            return BCrypt.Net.BCrypt.EnhancedVerify(answer.Trim().ToLower(), hashedAnswer, HashType.SHA384);
        }

        public async Task<bool> SetSecurityQuestionAsync(ApplicationUser user, string question, string answer)
        {
            if (user == null || string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer))
                return false;

            user.SecurityQuestion = question;
            user.SecurityAnswerHash = HashSecurityAnswer(answer);
            user.SecurityQuestionSetAt = DateTime.Now;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> VerifyUserSecurityAnswerAsync(ApplicationUser user, string answer)
        {
            if (user == null || string.IsNullOrEmpty(user.SecurityAnswerHash) || string.IsNullOrEmpty(answer))
            {
                Console.WriteLine($"❌ VERIFY FAILED: Missing data");
                return false;
            }

            Console.WriteLine($"🔐 VERIFYING: User={user.UserName}, AnswerLength={answer.Length}, HashExists={!string.IsNullOrEmpty(user.SecurityAnswerHash)}");

            var result = VerifySecurityAnswer(answer, user.SecurityAnswerHash);
            Console.WriteLine($"🔐 VERIFY RESULT: {result}");

            return result;
        }
    }
}