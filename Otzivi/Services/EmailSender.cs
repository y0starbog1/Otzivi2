using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Otzivi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendTwoFactorCodeAsync(string email, string code)
        {
            var subject = "Код двухфакторной аутентификации";
            var htmlMessage = $@"
        <h2>Код подтверждения для входа</h2>
        <p>Ваш код для входа в Otzivi: <strong>{code}</strong></p>
        <p>Код действителен в течение 10 минут.</p>
        <p>Если вы не запрашивали этот код, проигнорируйте это письмо.</p>
    ";

            await SendEmailAsync(email, subject, htmlMessage);
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Otzivi", "niki1304-str@mail.ru"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = htmlMessage
            };

            using var client = new SmtpClient();

            try
            {
                // Настройки для mail.ru
                await client.ConnectAsync("smtp.mail.ru", 587, false);

                // ⚠️ ВСТАВЬ СВОЙ SMTP ПАРОЛЬ ЗДЕСЬ ⚠️
                await client.AuthenticateAsync("niki1304-str@mail.ru", "GgKuyjic4hfy3M446FIa");

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"✅ Email отправлен на: {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки email: {ex.Message}");
                // Для тестирования выводим в консоль
                Console.WriteLine($"=== EMAIL CONTENT ===");
                Console.WriteLine($"To: {email}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Message: {htmlMessage}");
                Console.WriteLine($"===================");
            }
        }
    }
}