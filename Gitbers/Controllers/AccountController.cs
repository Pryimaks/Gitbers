using Gitbers.Data;
using Gitbers.Models;
using Gitbers.Models.ViewModels;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace Gitbers.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }


        // =========================
        // REGISTER
        // =========================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Перевіряємо, чи email уже використовується
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Користувач із таким email вже існує.");

                return View(model);
            }

            // Створюємо нового користувача
            var user = new User
            {
                Email = model.Email,
                FullName = model.FullName,
                Phone = model.Phone,

                // Новий користувач отримує роль Guest
                RoleId = 5,

                CreatedAt = DateTime.Now
            };

            // Хешуємо пароль
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.Password);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Реєстрація успішна. Тепер увійдіть у систему.";

            return RedirectToAction(nameof(Login));
        }


        // =========================
        // LOGIN
        // =========================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Знаходимо користувача разом із роллю
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Невірний email або пароль.");

                return View(model);
            }

            // Перевіряємо пароль
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Невірний email або пароль.");

                return View(model);
            }


            // =========================
            // CLAIMS
            // =========================

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Email),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role?.Name ?? "Guest")
            };


            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);


            var principal = new ClaimsPrincipal(identity);


            // Створюємо авторизаційну cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);


            return RedirectToAction(
                "Index",
                "Home");
        }


        // =========================
        // LOGOUT
        // =========================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }


        // =========================
        // ACCESS DENIED
        // =========================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }


        // =========================
        // PROFILE
        // =========================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            // Отримуємо ID користувача з Claims
            var userIdString =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction(nameof(Login));


            // Отримуємо користувача разом із роллю
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);


            if (user == null)
                return RedirectToAction(nameof(Login));


            return View(user);
        }
    }
}