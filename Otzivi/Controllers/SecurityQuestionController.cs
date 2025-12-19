using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Otzivi.Models;
using Otzivi.Services;
using Otzivi.ViewModels;

namespace Otzivi.Controllers
{
    public class SecurityQuestionController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ISecurityQuestionService _securityQuestionService;

        public SecurityQuestionController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ISecurityQuestionService securityQuestionService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _securityQuestionService = securityQuestionService;
        }

        // 🔐 СТРАНИЦА ПРОВЕРКИ СЕКРЕТНОГО ВОПРОСА
        [HttpGet]
        public async Task<IActionResult> Verify(string userId, string returnUrl = null, bool rememberMe = false)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsSecurityQuestionEnabled)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var model = new VerifySecurityQuestionViewModel
            {
                UserId = userId,
                SecurityQuestion = user.SecurityQuestion,
                ReturnUrl = returnUrl,
                RememberMe = rememberMe
            };

            return View(model);
        }

        // 🔐 ПРОВЕРКА ОТВЕТА НА СЕКРЕТНЫЙ ВОПРОС
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(VerifySecurityQuestionViewModel model)
        {
            Console.WriteLine($"🔐 2FA VERIFY: UserId={model.UserId}, Answer={model.SecurityAnswer}");

            // 🔧 ВРЕМЕННО ОТКЛЮЧАЕМ MODELSTATE VALIDATION
            // if (!ModelState.IsValid)
            // {
            //     Console.WriteLine($"❌ MODELSTATE ERRORS:");
            //     foreach (var key in ModelState.Keys)
            //     {
            //         var errors = ModelState[key].Errors;
            //         if (errors.Count > 0)
            //         {
            //             foreach (var error in errors)
            //             {
            //                 Console.WriteLine($"   - {key}: {error.ErrorMessage}");
            //             }
            //         }
            //     }
            //     return View(model);
            // }

            // Ручная проверка обязательных полей
            if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.SecurityAnswer))
            {
                Console.WriteLine($"❌ MISSING REQUIRED FIELDS");
                ModelState.AddModelError(string.Empty, "Заполните все обязательные поля");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null || !user.IsSecurityQuestionEnabled)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Проверяем ответ
            var isAnswerCorrect = await _securityQuestionService.VerifyUserSecurityAnswerAsync(user, model.SecurityAnswer);
            Console.WriteLine($"🔐 ANSWER VERIFICATION: {isAnswerCorrect}");

            if (isAnswerCorrect)
            {
                Console.WriteLine($"✅ 2FA SUCCESS: Logging in {user.UserName}");

                // Успешная проверка - выполняем вход
                await _signInManager.SignInAsync(user, model.RememberMe);
                return RedirectToAction("Index", "Product");
            }
            else
            {
                Console.WriteLine($"❌ 2FA FAILED: Wrong answer");
                ModelState.AddModelError(string.Empty, "Неверный ответ на секретный вопрос");
                model.SecurityQuestion = user.SecurityQuestion;
                return View(model);
            }
        }
    }
    }
    
    
