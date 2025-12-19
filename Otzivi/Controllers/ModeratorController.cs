using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Otzivi.Data;
using Otzivi.Models;

namespace Otzivi.Controllers
{
    [Authorize(Roles = "Moderator,Admin")]  // 👈 ДЛЯ МОДЕРАТОРОВ И АДМИНОВ
    public class ModeratorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ModeratorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔧 ПАНЕЛЬ МОДЕРАТОРА - УПРАВЛЕНИЕ ОТЗЫВАМИ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        // 🔧 СКРЫТИЕ ОТЗЫВА (МОДЕРАТОР МОЖЕТ СКРЫВАТЬ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideReview(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                review.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "✅ Отзыв скрыт";
            }

            return RedirectToAction("Index");
        }

        // 🔧 ВОССТАНОВЛЕНИЕ ОТЗЫВА
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShowReview(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                review.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "✅ Отзыв восстановлен";
            }

            return RedirectToAction("Index");
        }

        // 🔧 УДАЛЕНИЕ ОТЗЫВА (ТОЛЬКО АДМИН)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "✅ Отзыв удален";
            }

            return RedirectToAction("Index");
        }
    }
}