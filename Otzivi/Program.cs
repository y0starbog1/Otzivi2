using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Otzivi;
using Otzivi.Data;
using Otzivi.Middleware;
using Otzivi.Models;
using Otzivi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 🔧 НАСТРОЙКА IDENTITY
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 🔐 КОНФИГУРАЦИЯ COOKIE (1 МИНУТА ДЛЯ ТЕСТА)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1); // 👈 ИЗМЕНИТЬ НА 1 МИНУТУ
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
});

builder.Services.AddControllersWithViews();

// Регистрация сервисов
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, BCryptPasswordHasher>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISecurityAlertService, SecurityAlertService>();
builder.Services.AddScoped<ILoginAttemptService, LoginAttemptService>();
builder.Services.AddScoped<SignInManager<ApplicationUser>, CustomSignInManager>();
builder.Services.AddScoped<ISecurityQuestionService, SecurityQuestionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<SimpleCaptchaService>();

// СЕССИИ (1 МИНУТА ДЛЯ ТЕСТА)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1); // 👈 ИЗМЕНИТЬ НА 1 МИНУТУ
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Otzivi.Session";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Middleware в правильном порядке
app.UseMiddleware<TwoFactorRedirectMiddleware>();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<SessionTimeoutMiddleware>(); // 👈 НАШ НОВЫЙ MIDDLEWARE

// ⚠️ ВАЖНО: УБЕРИТЕ ВСЕ ЛИШНИЕ MapControllerRoute
// ОСТАВЬТЕ ТОЛЬКО ОДИН - ДЕФОЛТНЫЙ МАРШРУТ
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    await SeedData.InitializeAsync(context, scope.ServiceProvider);
}

app.Run();