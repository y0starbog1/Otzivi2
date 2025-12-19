using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Otzivi.Models;
using Otzivi.Services;
using Otzivi.ViewModels;

namespace Otzivi.Controllers
{
    [Authorize(Roles = "Admin")]  // 👈 ТОЛЬКО ДЛЯ АДМИНОВ
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRoleService _roleService;

        public AdminController(UserManager<ApplicationUser> userManager, IRoleService roleService)
        {
            _userManager = userManager;
            _roleService = roleService;
        }

        // 🔐 ПАНЕЛЬ УПРАВЛЕНИЯ АДМИНА
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userRoles = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _roleService.GetUserRolesAsync(user.Id);
                userRoles.Add(new UserWithRolesViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles
                });
            }

            return View(userRoles);
        }

        // 🔐 НАЗНАЧЕНИЕ РОЛИ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var result = await _roleService.AssignRoleAsync(userId, roleName);
            if (result)
            {
                TempData["StatusMessage"] = $"✅ Роль '{roleName}' успешно назначена";
            }
            else
            {
                TempData["StatusMessage"] = $"❌ Ошибка при назначении роли";
            }

            return RedirectToAction("Index");
        }

        // 🔐 УДАЛЕНИЕ РОЛИ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var result = await _roleService.RemoveRoleAsync(userId, roleName);
            if (result)
            {
                TempData["StatusMessage"] = $"✅ Роль '{roleName}' успешно удалена";
            }
            else
            {
                TempData["StatusMessage"] = $"❌ Ошибка при удалении роли";
            }

            return RedirectToAction("Index");
        }

        // 🔐 УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ (ТОЛЬКО ДЛЯ АДМИНА)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            // Не позволяем удалить самого себя
            if (userId == _userManager.GetUserId(User))
            {
                TempData["StatusMessage"] = "❌ Нельзя удалить свой собственный аккаунт";
                return RedirectToAction("Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["StatusMessage"] = "✅ Пользователь успешно удален";
                }
                else
                {
                    TempData["StatusMessage"] = "❌ Ошибка при удалении пользователя";
                }
            }

            return RedirectToAction("Index");
        }
    }
}