using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Otzivi.Data;
using Otzivi.Models;
using Otzivi.ViewModels;
using System.Security.Claims;

namespace Otzivi.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.ReviewLikes)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            bool hasUserReviewed = false;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                hasUserReviewed = product.Reviews.Any(r => r.UserId == userId);
            }

            var model = new ProductDetailsViewModel
            {
                Product = product,
                Reviews = product.Reviews.Where(r => r.IsActive).OrderByDescending(r => r.CreatedAt).ToList(),
                HasUserReviewed = hasUserReviewed
            };

            return View(model);
        }
    }
}