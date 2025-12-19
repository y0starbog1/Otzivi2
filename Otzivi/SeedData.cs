using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Otzivi.Data;
using Otzivi.Models;
using Otzivi.Services;

namespace Otzivi
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            // Создаем роли и администратора
            await CreateRolesAndAdmin(serviceProvider);

            // Добавляем тестовые товары если их нет
            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Product
                    {
                        Name = "Футболка Hysteric Glamour",
                        Brand = "Hysteric Glamour",
                        Description = "Культовая футболка с принтом",
                        Price = 12999.99m,
                        Category = "Одежда",
                        ImageUrl = "/images/product1.jpg"
                    },
                    new Product
                    {
                        Name = "Кроссовки Balenciaga Triple S",
                        Brand = "Balenciaga",
                        Description = "Массивные кроссовки с уникальным дизайном",
                        Price = 89999.99m,
                        Category = "Обувь",
                        ImageUrl = "/images/product2.jpg"
                    },
                    new Product
                    {
                        Name = "Куртка Stone Island",
                        Brand = "Stone Island",
                        Description = "Техническая куртка с компрессорным наполнителем",
                        Price = 75999.99m,
                        Category = "Верхняя одежда",
                        ImageUrl = "/images/product4.jpg"
                    },
                    new Product
                    {
                        Name = "Худи Comme des Garçons",
                        Brand = "Comme des Garçons",
                        Description = "Оверсайз худи с фирменным логотипом",
                        Price = 45999.99m,
                        Category = "Одежда",
                        ImageUrl = "/images/product5.jpg"
                    },
                    new Product
                    {
                        Name = "Кроссовки Nike Dunk Low",
                        Brand = "Nike",
                        Description = "Классические кроссовки для повседневной носки",
                        Price = 15999.99m,
                        Category = "Обувь",
                        ImageUrl = "/images/product3.jpg"
                    }
                );

                await context.SaveChangesAsync();
            }
        }

        private static async Task CreateRolesAndAdmin(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Создаем роли
            string[] roleNames = { "Admin", "Moderator", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"✅ Создана роль: {roleName}");
                }
            }

            // Создаем администратора
            var adminEmail = "admin@otzivi.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "System",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "Moderator");
                    Console.WriteLine($"✅ Создан администратор: {adminEmail}");
                }
            }

            // 🔧 СОЗДАЕМ МОДЕРАТОРА
            var moderatorEmail = "moderator@otzivi.com";
            var moderatorUser = await userManager.FindByEmailAsync(moderatorEmail);

            if (moderatorUser == null)
            {
                moderatorUser = new ApplicationUser
                {
                    UserName = moderatorEmail,
                    Email = moderatorEmail,
                    FirstName = "Moderator",
                    LastName = "System",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(moderatorUser, "Moderator123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(moderatorUser, "Moderator");
                    Console.WriteLine($"✅ Создан модератор: {moderatorEmail}");
                }
            }

            // Назначим роль User всем существующим пользователям
            var allUsers = userManager.Users.ToList();
            foreach (var user in allUsers)
            {
                var userRoles = await userManager.GetRolesAsync(user);
                if (!userRoles.Contains("User"))
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }
        }
    }
}