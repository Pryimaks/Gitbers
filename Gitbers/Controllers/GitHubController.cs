using System.Security.Claims;
using Gitbers.Data;
using Gitbers.Models;
using Gitbers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
    [Authorize]
    public class GitHubController : Controller
    {
        private readonly GitHubService _gitHubService;
        private readonly ApplicationDbContext _context;

        public GitHubController(
            GitHubService gitHubService,
            ApplicationDbContext context)
        {
            _gitHubService = gitHubService;
            _context = context;
        }

        // =========================================================
        // GET: /GitHub/Team/2
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Team(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Отримуємо тільки власну команду
            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync();

            if (team == null)
                return NotFound();

            var activities = await _context.GitHubActivities
                .Where(a => team.Members
                    .Select(m => m.TeamMemberId)
                    .Contains(a.TeamMemberId))
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            ViewBag.Activities = activities;

            return View(team);
        }

        // =========================================================
        // POST: /GitHub/Sync/2
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sync(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Перевіряємо, що команда належить поточному користувачу
            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == id &&
                    t.OwnerId == userId.Value)
                .Include(t => t.Members)
                .FirstOrDefaultAsync();

            if (team == null)
                return NotFound();

            if (!team.Members.Any())
            {
                TempData["Error"] =
                    "У команді немає учасників.";

                return RedirectToAction(
                    nameof(Team),
                    new { id });
            }

            var periodEnd = DateTime.UtcNow;
            var periodStart = periodEnd.AddDays(-7);

            foreach (var member in team.Members)
            {
                var repositories =
                    await _gitHubService.GetRepositoriesAsync(
                        member.GitHubUsername);

                int commitsCount = 0;
                int pullRequestsCount = 0;
                int issuesCount = 0;
                int reviewsCount = 0;

                foreach (var repository in repositories)
                {
                    var owner = repository.Owner?.Login;

                    if (string.IsNullOrWhiteSpace(owner))
                        continue;

                    // ==========================================
                    // 1. COMMITS
                    // ==========================================

                    var commits =
                        await _gitHubService.GetCommitsAsync(
                            owner,
                            repository.Name,
                            member.GitHubUsername,
                            periodStart,
                            periodEnd);

                    commitsCount += commits.Count;

                    // ==========================================
                    // 2. PULL REQUESTS
                    // ==========================================

                    var pullRequests =
                        await _gitHubService.GetPullRequestsAsync(
                            owner,
                            repository.Name);

                    var userPullRequests = pullRequests
                        .Where(pr =>
                            pr.User != null &&
                            pr.User.Login.Equals(
                                member.GitHubUsername,
                                StringComparison.OrdinalIgnoreCase))
                        .Where(pr =>
                            pr.CreatedAt >= periodStart &&
                            pr.CreatedAt <= periodEnd)
                        .ToList();

                    pullRequestsCount += userPullRequests.Count;

                    // ==========================================
                    // 3. ISSUES
                    // ==========================================

                    var issues =
                        await _gitHubService.GetIssuesAsync(
                            owner,
                            repository.Name,
                            member.GitHubUsername);

                    var userIssues = issues
                        .Where(issue =>
                            issue.CreatedAt >= periodStart &&
                            issue.CreatedAt <= periodEnd)
                        .Where(issue =>
                            issue.PullRequest == null)
                        .ToList();

                    issuesCount += userIssues.Count;

                    // ==========================================
                    // 4. CODE REVIEWS
                    // ==========================================

                    foreach (var pullRequest in pullRequests)
                    {
                        if (pullRequest.CreatedAt > periodEnd)
                            continue;

                        var reviews =
                            await _gitHubService
                                .GetPullRequestReviewsAsync(
                                    owner,
                                    repository.Name,
                                    pullRequest.Number);

                        var userReviews = reviews
                            .Where(review =>
                                review.User != null &&
                                review.User.Login.Equals(
                                    member.GitHubUsername,
                                    StringComparison.OrdinalIgnoreCase))
                            .Where(review =>
                                review.SubmittedAt.HasValue &&
                                review.SubmittedAt.Value >= periodStart &&
                                review.SubmittedAt.Value <= periodEnd)
                            .ToList();

                        reviewsCount += userReviews.Count;
                    }
                }

                // ==========================================
                // Збереження результату
                // ==========================================

                var activity = new GitHubActivity
                {
                    TeamMemberId = member.TeamMemberId,
                    GitHubUsername = member.GitHubUsername,
                    ActivityDate = DateTime.Now,

                    CommitsCount = commitsCount,
                    PullRequestsCount = pullRequestsCount,
                    IssuesCount = issuesCount,
                    ReviewsCount = reviewsCount,

                    CreatedAt = DateTime.Now
                };

                _context.GitHubActivities.Add(activity);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "GitHub активність команди успішно оновлено.";

            return RedirectToAction(
                nameof(Team),
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