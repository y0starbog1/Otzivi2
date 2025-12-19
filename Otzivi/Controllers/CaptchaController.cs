using Microsoft.AspNetCore.Mvc;
using Otzivi.Services;

namespace Otzivi.Controllers
{
    public class CaptchaController : Controller
    {
        private readonly SimpleCaptchaService _captchaService;

        public CaptchaController(SimpleCaptchaService captchaService)
        {
            _captchaService = captchaService;
        }

        [HttpGet]
        [Route("Captcha/GetImage")]
        public IActionResult GetImage()
        {
            try
            {
                Console.WriteLine("=== ГЕНЕРАЦИЯ КАПЧИ ===");
                Console.WriteLine($"Сессия доступна: {HttpContext.Session != null}");
                Console.WriteLine($"ID сессии: {HttpContext.Session?.Id}");

                // Генерируем капчу
                var captchaImage = _captchaService.GenerateCaptcha();
                Console.WriteLine($"Капча сгенерирована, размер: {captchaImage.Length} байт");

                // Устанавливаем заголовки для отключения кэширования
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                return File(captchaImage, "image/png");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА генерации капчи: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // Возвращаем простую ошибку в виде изображения
                return NotFound();
            }
        }
    }
}