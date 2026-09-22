using System.Security.Claims;
using Gitbers.Data;
using Gitbers.Models;
using Gitbers.Models.ViewModels;
using Gitbers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
    [Authorize]
    public class TeamController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GitHubService _gitHubService;

        public TeamController(
            ApplicationDbContext context,
            GitHubService gitHubService)
        {
            _context = context;
            _gitHubService = gitHubService;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var teams = await _context.Teams
                .Where(t => t.OwnerId == userId.Value)
                .Include(t => t.Members)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(teams);
        }

        // ============================================================
        // DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync();

            if (team == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // Активне опитування
            // --------------------------------------------------------

            var activeSurvey = await _context.Surveys
                .Where(s =>
                    s.TeamId == team.TeamId &&
                    s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.ActiveSurvey = activeSurvey;

            return View(team);
        }

        // ============================================================
        // CREATE - GET
        // ============================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(
                new TeamCreateViewModel());
        }

        // ============================================================
        // CREATE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TeamCreateViewModel model)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // --------------------------------------------------------
            // Перевірка GitHub-проєкту
            // --------------------------------------------------------

            GitHubRepository? githubRepository = null;

            if (!string.IsNullOrWhiteSpace(model.GitHubOwner) &&
                !string.IsNullOrWhiteSpace(model.GitHubRepository))
            {
                githubRepository =
                    await _gitHubService.GetRepositoryAsync(
                        model.GitHubOwner,
                        model.GitHubRepository);

                if (githubRepository == null)
                {
                    ModelState.AddModelError(
                        "GitHubRepository",
                        "Не вдалося знайти вибраний GitHub-репозиторій.");

                    return View(model);
                }
            }

            // --------------------------------------------------------
            // Створення команди
            // --------------------------------------------------------

            var team = new Team
            {
                Name = model.Name,
                Description = model.Description,

                GitHubOwner = model.GitHubOwner,
                GitHubRepository = model.GitHubRepository,

                OwnerId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);

            await _context.SaveChangesAsync();

            // --------------------------------------------------------
            // Якщо GitHub-проєкт вибрано —
            // автоматично отримуємо учасників
            // --------------------------------------------------------

            int addedMembersCount = 0;

            if (githubRepository != null)
            {
                var collaborators =
                    await _gitHubService
                        .GetRepositoryCollaboratorsAsync(
                            model.GitHubOwner!,
                            model.GitHubRepository!);

                foreach (var collaborator in collaborators)
                {
                    if (string.IsNullOrWhiteSpace(
                        collaborator.Login))
                    {
                        continue;
                    }

                    // Перевірка, щоб не додати дублікати
                    var alreadyExists =
                        await _context.TeamMembers
                            .AnyAsync(m =>
                                m.TeamId == team.TeamId &&
                                m.GitHubUsername.ToLower()
                                    == collaborator.Login.ToLower());

                    if (alreadyExists)
                    {
                        continue;
                    }

                    var teamMember = new TeamMember
                    {
                        TeamId = team.TeamId,
                        GitHubUsername =
                            collaborator.Login
                    };

                    _context.TeamMembers.Add(
                        teamMember);

                    addedMembersCount++;
                }

                await _context.SaveChangesAsync();
            }

            // --------------------------------------------------------
            // Повідомлення користувачу
            // --------------------------------------------------------

            if (githubRepository != null)
            {
                TempData["Success"] =
                    $"Команду успішно створено. " +
                    $"GitHub-проєкт: " +
                    $"{model.GitHubOwner}/{model.GitHubRepository}. " +
                    $"Автоматично додано учасників: " +
                    $"{addedMembersCount}.";
            }
            else
            {
                TempData["Success"] =
                    "Команду успішно створено.";
            }

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = team.TeamId
                });
        }

        // ============================================================
        // EDIT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value)
                .FirstOrDefaultAsync();

            if (team == null)
            {
                return NotFound();
            }

            var model = new TeamCreateViewModel
            {
                Name = team.Name,
                Description = team.Description,
                GitHubOwner = team.GitHubOwner,
                GitHubRepository = team.GitHubRepository
            };

            ViewBag.TeamId = team.TeamId;

            return View(model);
        }

        // ============================================================
        // EDIT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            TeamCreateViewModel model)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TeamId = id;

                return View(model);
            }

            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value)
                .Include(t => t.Members)
                .FirstOrDefaultAsync();

            if (team == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // Перевірка нового GitHub-проєкту
            // --------------------------------------------------------

            GitHubRepository? githubRepository = null;

            if (!string.IsNullOrWhiteSpace(
                    model.GitHubOwner) &&
                !string.IsNullOrWhiteSpace(
                    model.GitHubRepository))
            {
                githubRepository =
                    await _gitHubService.GetRepositoryAsync(
                        model.GitHubOwner,
                        model.GitHubRepository);

                if (githubRepository == null)
                {
                    ModelState.AddModelError(
                        "GitHubRepository",
                        "Не вдалося знайти вибраний GitHub-репозиторій.");

                    ViewBag.TeamId = id;

                    return View(model);
                }
            }

            bool repositoryChanged =
                !string.Equals(
                    team.GitHubOwner,
                    model.GitHubOwner,
                    StringComparison.OrdinalIgnoreCase)
                ||
                !string.Equals(
                    team.GitHubRepository,
                    model.GitHubRepository,
                    StringComparison.OrdinalIgnoreCase);

            team.Name = model.Name;
            team.Description = model.Description;
            team.GitHubOwner = model.GitHubOwner;
            team.GitHubRepository = model.GitHubRepository;

            await _context.SaveChangesAsync();

            // --------------------------------------------------------
            // Якщо репозиторій змінився —
            // додаємо нових учасників
            // --------------------------------------------------------

            int addedMembersCount = 0;

            if (repositoryChanged &&
                githubRepository != null)
            {
                var collaborators =
                    await _gitHubService
                        .GetRepositoryCollaboratorsAsync(
                            model.GitHubOwner!,
                            model.GitHubRepository!);

                foreach (var collaborator in collaborators)
                {
                    if (string.IsNullOrWhiteSpace(
                        collaborator.Login))
                    {
                        continue;
                    }

                    var exists =
                        team.Members.Any(m =>
                            m.GitHubUsername.Equals(
                                collaborator.Login,
                                StringComparison.OrdinalIgnoreCase));

                    if (exists)
                    {
                        continue;
                    }

                    _context.TeamMembers.Add(
                        new TeamMember
                        {
                            TeamId = team.TeamId,
                            GitHubUsername =
                                collaborator.Login
                        });

                    addedMembersCount++;
                }

                await _context.SaveChangesAsync();
            }

            TempData["Success"] =
                "Команду успішно оновлено." +
                (
                    repositoryChanged
                        ? $" Додано нових GitHub-учасників: {addedMembersCount}."
                        : ""
                );

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // ADD MEMBER - ЗАЛИШАЄМО ДЛЯ РУЧНОГО ДОДАВАННЯ
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(
            int teamId,
            string githubUsername)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == teamId &&
                    t.OwnerId == userId.Value)
                .FirstOrDefaultAsync();

            if (team == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(
                githubUsername))
            {
                TempData["Error"] =
                    "Вкажіть GitHub username.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = teamId });
            }

            githubUsername =
                githubUsername.Trim();

            var exists =
                await _context.TeamMembers
                    .AnyAsync(m =>
                        m.TeamId == teamId &&
                        m.GitHubUsername.ToLower()
                            == githubUsername.ToLower());

            if (exists)
            {
                TempData["Error"] =
                    "Такий учасник вже є в команді.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = teamId });
            }

            var member = new TeamMember
            {
                TeamId = teamId,
                GitHubUsername = githubUsername
            };

            _context.TeamMembers.Add(member);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Учасника додано.";

            return RedirectToAction(
                nameof(Details),
                new { id = teamId });
        }

        // ============================================================
        // DELETE TEAM
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value)
                .FirstOrDefaultAsync();

            if (team == null)
            {
                return NotFound();
            }

            _context.Teams.Remove(team);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Команду видалено.";

            return RedirectToAction(
                nameof(Index));
        }

        // ============================================================
        // CURRENT USER
        // ============================================================

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