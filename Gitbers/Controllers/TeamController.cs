using Gitbers.Data;
using Gitbers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
    public class TeamController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeamController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Team
        public async Task<IActionResult> Index()
        {
            var teams = await _context.Teams
                .Include(t => t.Members)
                .ToListAsync();

            return View(teams);
        }

        // GET: /Team/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Team/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Gitbers.Models.Team team)
        {
            if (!ModelState.IsValid)
            {
                return View(team);
            }

            team.CreatedAt = DateTime.UtcNow;

            _context.Teams.Add(team);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Team/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var team = await _context.Teams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
                return NotFound();

            return View(team);
        }

        // GET: /Team/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var team = await _context.Teams.FindAsync(id);

            if (team == null)
                return NotFound();

            return View(team);
        }

        // POST: /Team/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Team team)
        {
            if (id != team.TeamId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(team);

            try
            {
                _context.Update(team);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Teams.Any(t => t.TeamId == id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Team/Delete/1
        public async Task<IActionResult> Delete(int id)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
                return NotFound();

            return View(team);
        }

        // POST: /Team/Delete/1
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
                return NotFound();

            _context.Teams.Remove(team);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Team/AddMember/2
        public async Task<IActionResult> AddMember(int id)
        {
            var team = await _context.Teams.FindAsync(id);

            if (team == null)
                return NotFound();

            var existingUserIds = await _context.TeamMembers
                .Where(tm => tm.TeamId == id)
                .Select(tm => tm.UserId)
                .ToListAsync();

            var users = await _context.Users
                .Where(u => !existingUserIds.Contains(u.Id))
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.Team = team;

            return View(new TeamMember
            {
                TeamId = id
            });
        }


        // POST: /Team/AddMember/2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int id, TeamMember member)
        {
            var team = await _context.Teams.FindAsync(id);

            if (team == null)
                return NotFound();

            // Прив'язуємо учасника саме до команди з URL
            member.TeamId = id;

            // Перевіряємо користувача
            var user = await _context.Users.FindAsync(member.UserId);

            if (user == null)
            {
                ModelState.AddModelError("UserId", "Користувача не знайдено.");
            }

            // Перевіряємо, чи користувач уже є в команді
            var alreadyExists = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == id && tm.UserId == member.UserId);

            if (alreadyExists)
            {
                ModelState.AddModelError("UserId", "Цей користувач уже є учасником команди.");
            }

            if (string.IsNullOrWhiteSpace(member.GitHubUsername))
            {
                ModelState.AddModelError(
                    "GitHubUsername",
                    "Вкажіть GitHub username."
                );
            }

            if (!ModelState.IsValid)
            {
                var existingUserIds = await _context.TeamMembers
                    .Where(tm => tm.TeamId == id)
                    .Select(tm => tm.UserId)
                    .ToListAsync();

                ViewBag.Users = await _context.Users
                    .Where(u => !existingUserIds.Contains(u.Id))
                    .ToListAsync();

                ViewBag.Team = team;

                return View(member);
            }

            member.GitHubUsername = member.GitHubUsername.Trim();
            member.AddedAt = DateTime.Now;

            _context.TeamMembers.Add(member);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}