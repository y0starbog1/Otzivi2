using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Otzivi.Data;
using Otzivi.Models;
using Otzivi.ViewModels;
using System.Security.Claims;

namespace Otzivi.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(ApplicationDbContext context, ILogger<ReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "Вы уже оставляли отзыв на этот продукт";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var model = new CreateReviewViewModel
            {
                ProductId = productId,
                ProductName = product.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    var existingReview = await _context.Reviews
                        .FirstOrDefaultAsync(r => r.ProductId == model.ProductId && r.UserId == userId);

                    if (existingReview != null)
                    {
                        ModelState.AddModelError("", "Вы уже оставляли отзыв на этот продукт");
                        return View(model);
                    }

                    var review = new Review
                    {
                        ProductId = model.ProductId,
                        UserId = userId,
                        Rating = model.Rating,
                        Title = model.Title,
                        Content = model.Content,
                        CreatedAt = DateTime.UtcNow,
                        IsVerifiedPurchase = model.IsVerifiedPurchase,
                        IsActive = true,
                        LikesCount = 0,
                        DislikesCount = 0
                    };

                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Отзыв успешно добавлен!";
                    return RedirectToAction("Details", "Product", new { id = model.ProductId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating review");
                    ModelState.AddModelError("", $"Ошибка при создании отзыва: {ex.Message}");
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LikeReview(int reviewId, bool isLike)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var existingLike = await _context.ReviewLikes
                    .FirstOrDefaultAsync(rl => rl.ReviewId == reviewId && rl.UserId == userId);

                var review = await _context.Reviews.FindAsync(reviewId);

                if (review == null)
                {
                    return Json(new { success = false, message = "Review not found" });
                }

                if (existingLike != null)
                {
                    if (existingLike.IsLike == isLike)
                    {
                        _context.ReviewLikes.Remove(existingLike);
                        if (isLike) review.LikesCount--;
                        else review.DislikesCount--;
                    }
                    else
                    {
                        existingLike.IsLike = isLike;
                        if (isLike)
                        {
                            review.LikesCount++;
                            review.DislikesCount--;
                        }
                        else
                        {
                            review.LikesCount--;
                            review.DislikesCount++;
                        }
                    }
                }
                else
                {
                    var reviewLike = new ReviewLike
                    {
                        ReviewId = reviewId,
                        UserId = userId,
                        IsLike = isLike,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ReviewLikes.Add(reviewLike);
                    if (isLike) review.LikesCount++;
                    else review.DislikesCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    likesCount = review.LikesCount,
                    dislikesCount = review.DislikesCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking review");
                return Json(new { success = false, message = "Ошибка при оценке отзыва" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reviews = await _context.Reviews
                .Where(r => r.UserId == userId)
                .Include(r => r.Product)
                .Include(r => r.ReviewLikes)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }
    }
}   