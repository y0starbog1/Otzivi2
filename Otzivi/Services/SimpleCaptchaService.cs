using Microsoft.AspNetCore.Http;
using System.Drawing;
using System.Drawing.Imaging;

namespace Otzivi.Services
{
    public class SimpleCaptchaService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Random _random = new();

        public SimpleCaptchaService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Генерируем случайный текст (4 символа)
        private string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[_random.Next(s.Length)]).ToArray());

            Console.WriteLine($"Сгенерирован код капчи: {code}");
            return code;
        }

        // Создаем изображение капчи и сохраняем код
        public byte[] GenerateCaptcha()
        {
            try
            {
                Console.WriteLine("=== Начало генерации капчи ===");

                var code = GenerateRandomCode();
                Console.WriteLine($"Код: {code}");

                using var bitmap = new Bitmap(150, 50);
                using var graphics = Graphics.FromImage(bitmap);

                // Белый фон
                graphics.Clear(Color.White);
                Console.WriteLine("Фон создан");

                // Рисуем текст
                using var font = new Font("Arial", 20, FontStyle.Bold);
                using var brush = new SolidBrush(Color.Black);

                graphics.DrawString(code, font, brush, new PointF(10, 10));
                Console.WriteLine("Текст нарисован");

                // Сохраняем код в сессии
                var session = _httpContextAccessor.HttpContext.Session;
                if (session != null)
                {
                    session.SetString("CaptchaCode", code);
                    session.SetString("CaptchaTime", DateTime.Now.Ticks.ToString());
                    Console.WriteLine($"Код сохранен в сессии: {code}");
                }
                else
                {
                    Console.WriteLine("❌ ОШИБКА: Сессия не доступна!");
                }

                // Конвертируем в байты
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                var bytes = stream.ToArray();

                Console.WriteLine($"Капча создана, размер: {bytes.Length} байт");
                return bytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА в GenerateCaptcha: {ex.Message}");
                throw;
            }
        }

        // Проверяем введенный код (ВАЖНО: метод принимает ТОЛЬКО userInput)
        public bool ValidateCaptcha(string userInput)
        {
            Console.WriteLine($"=== ПРОВЕРКА КАПЧИ ===");
            Console.WriteLine($"Введенный код: {userInput}");

            var session = _httpContextAccessor.HttpContext.Session;
            if (session == null)
            {
                Console.WriteLine("❌ Сессия не доступна!");
                return false;
            }

            var savedCode = session.GetString("CaptchaCode");
            var savedTime = session.GetString("CaptchaTime");

            Console.WriteLine($"Сохраненный код: {savedCode}");
            Console.WriteLine($"Сохраненное время: {savedTime}");

            // Очищаем после проверки
            session.Remove("CaptchaCode");
            session.Remove("CaptchaTime");

            if (string.IsNullOrEmpty(savedCode) || string.IsNullOrEmpty(savedTime))
            {
                Console.WriteLine("❌ Нет сохраненного кода или времени");
                return false;
            }

            // Проверяем время (5 минут)
            var time = new DateTime(long.Parse(savedTime));
            if (DateTime.Now - time > TimeSpan.FromMinutes(5))
            {
                Console.WriteLine("❌ Код устарел (больше 5 минут)");
                return false;
            }

            // Проверяем код (без учета регистра)
            var result = string.Equals(savedCode, userInput, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine($"Результат проверки: {result}");

            return result;
        }

        // 📌 ДОБАВЬТЕ ЭТОТ МЕТОД для обратной совместимости
        public bool ValidateCaptcha(string userInput, string sessionCaptcha)
        {
            // Просто игнорируем второй параметр и используем текущий метод
            return ValidateCaptcha(userInput);
        }
    }
}