using System.Security.Claims;
using Gitbers.Data;
using Gitbers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
    [Authorize]
    public class TeamController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeamController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Team
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var teams = await _context.Teams
                .Where(t => t.OwnerId == userId.Value)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(teams);
        }

        // =========================================================
        // GET: /Team/Create
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // =========================================================
        // POST: /Team/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team team)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(team);

            // OwnerId НІКОЛИ не беремо з форми
            team.OwnerId = userId.Value;
            team.CreatedAt = DateTime.UtcNow;

            _context.Teams.Add(team);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // GET: /Team/Details/5
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == id.Value &&
                    t.OwnerId == userId.Value)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync();

            if (team == null)
                return NotFound();

            return View(team);
        }

        // =========================================================
        // GET: /Team/Edit/5
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var team = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == id.Value &&
                    t.OwnerId == userId.Value);

            if (team == null)
                return NotFound();

            return View(team);
        }

        // =========================================================
        // POST: /Team/Edit/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Team team)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (id != team.TeamId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(team);

            // Отримуємо реальну команду тільки власника
            var existingTeam = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value);

            if (existingTeam == null)
                return NotFound();

            // Оновлюємо тільки дозволені поля
            existingTeam.Name = team.Name;
            existingTeam.Description = team.Description;
            existingTeam.GitHubOrganization =
                team.GitHubOrganization;

            // OwnerId НЕ змінюємо
            // CreatedAt НЕ змінюємо

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // GET: /Team/Delete/5
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var team = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value);

            if (team == null)
                return NotFound();

            return View(team);
        }

        // =========================================================
        // POST: /Team/Delete/5
        // =========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var team = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value);

            if (team == null)
                return NotFound();

            _context.Teams.Remove(team);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // GET: /Team/AddMember/2
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> AddMember(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var team = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value);

            if (team == null)
                return NotFound();

            var existingUserIds = await _context.TeamMembers
                .Where(tm => tm.TeamId == id)
                .Select(tm => tm.UserId)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => !existingUserIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.Team = team;

            return View(new TeamMember
            {
                TeamId = id
            });
        }

        // =========================================================
        // POST: /Team/AddMember/2
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(
            int id,
            TeamMember member)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Перевіряємо, що команда належить поточному користувачу
            var team = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value);

            if (team == null)
                return NotFound();

            // Команда береться тільки з URL
            member.TeamId = id;

            // Перевіряємо користувача
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == member.UserId);

            if (user == null)
            {
                ModelState.AddModelError(
                    "UserId",
                    "Користувача не знайдено.");
            }

            // Перевіряємо, чи користувач уже є в команді
            var alreadyExists = await _context.TeamMembers
                .AnyAsync(tm =>
                    tm.TeamId == id &&
                    tm.UserId == member.UserId);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    "UserId",
                    "Цей користувач уже є учасником команди.");
            }

            // Перевіряємо GitHub username
            if (string.IsNullOrWhiteSpace(
                member.GitHubUsername))
            {
                ModelState.AddModelError(
                    "GitHubUsername",
                    "Вкажіть GitHub username.");
            }

            if (!ModelState.IsValid)
            {
                var existingUserIds =
                    await _context.TeamMembers
                        .Where(tm => tm.TeamId == id)
                        .Select(tm => tm.UserId)
                        .ToListAsync();

                ViewBag.Users = await _context.Users
                    .Where(u =>
                        !existingUserIds.Contains(u.Id))
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                ViewBag.Team = team;

                return View(member);
            }

            member.GitHubUsername =
                member.GitHubUsername.Trim();

            member.AddedAt = DateTime.Now;

            _context.TeamMembers.Add(member);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // Допоміжний метод
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

        // =========================================================
        // GET: /Team/ConnectViber/5
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ConnectViber(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var member = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Include(tm => tm.User)
                .FirstOrDefaultAsync(tm =>
                    tm.TeamMemberId == id &&
                    tm.Team != null &&
                    tm.Team.OwnerId == userId.Value);

            if (member == null)
                return NotFound();

            return View(member);
        }


        // =========================================================
        // POST: /Team/ConnectViber/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConnectViber(
    int id,
    string phoneNumber)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var member = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Include(tm => tm.User)
                .FirstOrDefaultAsync(tm =>
                    tm.TeamMemberId == id &&
                    tm.Team != null &&
                    tm.Team.OwnerId == userId.Value);

            if (member == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError(
                    "phoneNumber",
                    "Введіть номер телефону.");

                return View(member);
            }

            phoneNumber = phoneNumber.Trim();

            if (phoneNumber.Length > 20)
            {
                ModelState.AddModelError(
                    "phoneNumber",
                    "Номер телефону занадто довгий.");

                return View(member);
            }

            // Генеруємо одноразовий код підключення
            var connectionCode =
                Random.Shared.Next(100000, 999999).ToString();

            member.PhoneNumber = phoneNumber;
            member.ViberUserId = null;
            member.ViberConnected = false;
            member.ViberConnectionCode = connectionCode;
            member.ViberConnectionCodeExpiresAt =
                DateTime.UtcNow.AddMinutes(30);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(ConnectViber),
                new { id = member.TeamMemberId });
        }
    }
}