using System.Security.Claims;
using Gitbers.Data;
using Gitbers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
    [Authorize]
    public class MetricsController : Controller
    {
        private readonly MetricsService _metricsService;
        private readonly ApplicationDbContext _context;

        public MetricsController(
            MetricsService metricsService,
            ApplicationDbContext context)
        {
            _metricsService = metricsService;
            _context = context;
        }

        // =========================================================
        // GET: /Metrics/Index/2
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Отримуємо тільки власну команду
            var team = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value);

            if (team == null)
                return NotFound();

            var latestSnapshot = await _context.MetricSnapshots
                .Where(m => m.TeamId == id)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.Team = team;

            return View(latestSnapshot);
        }

        // =========================================================
        // GET: /Metrics/History/2
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Перевіряємо власника команди
            var team = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value);

            if (team == null)
                return NotFound();

            var snapshots = await _context.MetricSnapshots
                .Where(m => m.TeamId == id)
                .OrderBy(m => m.PeriodStart)
                .ToListAsync();

            ViewBag.Team = team;

            return View(snapshots);
        }

        // =========================================================
        // POST: /Metrics/Calculate/2
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Дуже важливо:
            // не дозволяємо розраховувати метрики чужої команди
            var teamExists = await _context.Teams
                .AnyAsync(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value);

            if (!teamExists)
                return NotFound();

            var periodEnd = DateTime.Now;
            var periodStart = periodEnd.AddDays(-7);

            var snapshot =
                await _metricsService.CalculateTeamMetricsAsync(
                    id,
                    periodStart,
                    periodEnd);

            if (snapshot == null)
            {
                TempData["Error"] =
                    "Недостатньо даних для розрахунку метрик. " +
                    "Спочатку виконайте синхронізацію GitHub.";

                return RedirectToAction(
                    nameof(Index),
                    new { id });
            }

            TempData["Success"] =
                "Метрики команди успішно розраховано.";

            return RedirectToAction(
                nameof(Index),
                new { id });
        }

        // =========================================================
        // Поточний користувач
        // =========================================================
        private int? GetCurrentUserId()
        {
            var userIdString =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (int.TryParse(
                userIdString,
                out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}