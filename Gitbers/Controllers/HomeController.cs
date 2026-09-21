using Gitbers.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Gitbers.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdString =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var teams = await _context.Teams
                .Where(t => t.OwnerId == userId)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(teams);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }


        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(
                new Gitbers.Models.ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}