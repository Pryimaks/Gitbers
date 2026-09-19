using Gitbers.Data;
using Gitbers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
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

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
                return NotFound();

            var latestSnapshot = await _context.MetricSnapshots
                .Where(m => m.TeamId == id)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.Team = team;

            return View(latestSnapshot);
        }

        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
                return NotFound();

            var snapshots = await _context.MetricSnapshots
                .Where(m => m.TeamId == id)
                .OrderBy(m => m.PeriodStart)
                .ToListAsync();

            ViewBag.Team = team;

            return View(snapshots);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(int id)
        {
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
    }
}