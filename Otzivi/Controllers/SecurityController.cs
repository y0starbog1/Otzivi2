using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otzivi.Services;
using System.Threading.Tasks;

namespace Otzivi.Controllers
{
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly ISecurityAlertService _securityAlertService;

        public SecurityController(ISecurityAlertService securityAlertService)
        {
            _securityAlertService = securityAlertService;
        }

        // История безопасности пользователя
        [HttpGet]
        public async Task<IActionResult> MySecurityEvents()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var events = await _securityAlertService.GetUserSecurityEventsAsync(userId, 50);
            return View(events);
        }

        // Панель безопасности для админа
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminSecurityEvents()
        {
            var events = await _securityAlertService.GetAllSecurityEventsAsync(100);
            return View(events);
        }
    }
}