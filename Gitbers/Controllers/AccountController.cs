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
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }


        // ==========================================
        // REGISTER
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // Перевірка email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == model.Email.ToLower());


            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Користувач з таким email вже існує.");

                return View(model);
            }


            // Роль звичайного користувача
            var role = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.Name == "Developer");


            if (role == null)
            {
                ModelState.AddModelError(
                    "",
                    "Не вдалося визначити роль користувача.");

                return View(model);
            }


            var user = new User
            {
                Email = model.Email.Trim().ToLower(),
                FullName = model.FullName?.Trim(),
                Phone = model.Phone?.Trim(),
                RoleId = role.RoleId,
                CreatedAt = DateTime.Now
            };


            // Хешування пароля
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    model.Password);


            _context.Users.Add(user);

            await _context.SaveChangesAsync();


            // Автоматично авторизуємо користувача
            await SignInUser(user);


            return RedirectToAction(
                "Index",
                "Home");
        }


        // ==========================================
        // LOGIN
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(
            string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            ViewBag.ReturnUrl = returnUrl;

            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() ==
                    model.Email.ToLower());


            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Неправильний email або пароль.");

                return View(model);
            }


            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    model.Password);


            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Неправильний email або пароль.");

                return View(model);
            }


            await SignInUser(user);


            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }


            return RedirectToAction(
                "Index",
                "Home");
        }


        // ==========================================
        // LOGOUT
        // ==========================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            return RedirectToAction(
                nameof(Login));
        }


        // ==========================================
        // ACCESS DENIED
        // ==========================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        // ==========================================
        // PRIVATE SIGN IN
        // ==========================================

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName ?? user.Email)
            };


            if (user.Role != null)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        user.Role.Name));
            }


            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults
                    .AuthenticationScheme);


            var principal =
                new ClaimsPrincipal(identity);


            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc =
                        DateTimeOffset.UtcNow.AddHours(8)
                });
        }
    }
}