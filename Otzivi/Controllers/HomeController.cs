using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Otzivi.Models;

namespace Otzivi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Существующий метод
        public IActionResult Privacy()
        {
            return View();
        }

        // НОВЫЙ МЕТОД: Политика конфиденциальности
        [Route("/privacy-policy")]
        public IActionResult PrivacyPolicy()
        {
            ViewData["LastUpdated"] = "15 марта 2024";
            return View();
        }

        // НОВЫЙ МЕТОД: Условия пользовательского соглашения
        [Route("/terms-of-service")]
        public IActionResult TermsOfService()
        {
            ViewData["EffectiveDate"] = "15 марта 2024";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}