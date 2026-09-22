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
        private readonly ILogger<GitHubController> _logger;

        public GitHubController(
            GitHubService gitHubService,
            ApplicationDbContext context,
            ILogger<GitHubController> logger)
        {
            _gitHubService = gitHubService;
            _context = context;
            _logger = logger;
        }


        // ============================================================
        // GITHUB REPOSITORIES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Repositories()
        {
            try
            {
                _logger.LogInformation(
                    "Отримання списку GitHub-репозиторіїв.");

                var repositories =
                    await _gitHubService
                        .GetMyRepositoriesAsync();

                _logger.LogInformation(
                    "GitHub повернув {Count} репозиторіїв.",
                    repositories.Count);

                return Json(new
                {
                    success = true,
                    repositories
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Помилка HTTP під час отримання GitHub-репозиторіїв.");

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = ex.Message
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Помилка під час отримання GitHub-репозиторіїв.");

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Не вдалося отримати репозиторії GitHub: " +
                            ex.Message
                    });
            }
        }


        // ============================================================
        // TEAM GITHUB PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Team(int id)
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
                return NotFound();

            var activities =
                await _context.GitHubActivities
                    .Where(a =>
                        team.Members
                            .Select(m => m.TeamMemberId)
                            .Contains(a.TeamMemberId))
                    .OrderByDescending(
                        a => a.ActivityDate)
                    .ToListAsync();

            ViewBag.Activities = activities;

            return View(team);
        }


        // ============================================================
        // CONNECT REPOSITORY TO TEAM
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConnectRepository(
            int teamId,
            string owner,
            string repository)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (string.IsNullOrWhiteSpace(owner) ||
                string.IsNullOrWhiteSpace(repository))
            {
                TempData["Error"] =
                    "Необхідно вибрати GitHub-репозиторій.";

                return RedirectToAction(
                    "Details",
                    "Team",
                    new { id = teamId });
            }

            owner = owner.Trim();
            repository = repository.Trim();

            var team = await _context.Teams
                .Where(t =>
                    t.TeamId == teamId &&
                    t.OwnerId == userId.Value)
                .Include(t => t.Members)
                .FirstOrDefaultAsync();

            if (team == null)
                return NotFound();


            // ========================================================
            // ПЕРЕВІРКА РЕПОЗИТОРІЮ
            // ========================================================

            _logger.LogInformation(
                "Перевірка GitHub-репозиторію {Owner}/{Repository}.",
                owner,
                repository);

            var githubRepository =
                await _gitHubService.GetRepositoryAsync(
                    owner,
                    repository);

            if (githubRepository == null)
            {
                TempData["Error"] =
                    "Не вдалося знайти вибраний GitHub-репозиторій. " +
                    "Перевір доступ до нього.";

                return RedirectToAction(
                    "Details",
                    "Team",
                    new { id = teamId });
            }


            team.GitHubOwner = owner;
            team.GitHubRepository = repository;


            // ========================================================
            // COLLABORATORS
            // ========================================================

            var collaborators =
                await _gitHubService
                    .GetRepositoryCollaboratorsAsync(
                        owner,
                        repository);

            _logger.LogInformation(
                "ConnectRepository: collaborators = {Count}",
                collaborators.Count);


            // ========================================================
            // CONTRIBUTORS
            // ========================================================

            var contributors =
                await _gitHubService
                    .GetRepositoryContributorsAsync(
                        owner,
                        repository);

            _logger.LogInformation(
                "ConnectRepository: contributors = {Count}",
                contributors.Count);


            // ========================================================
            // ОБ'ЄДНАННЯ GITHUB-КОРИСТУВАЧІВ
            // ========================================================

            var githubUsers =
                collaborators
                    .Concat(contributors)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Login))
                    .GroupBy(
                        x => x.Login,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();


            _logger.LogInformation(
                "Унікальних GitHub-користувачів: {Count}",
                githubUsers.Count);


            int addedCount = 0;


            // ========================================================
            // ДОДАВАННЯ GITHUB-КОРИСТУВАЧІВ
            // ========================================================

            foreach (var githubUser in githubUsers)
            {
                var login =
                    githubUser.Login?.Trim();

                if (string.IsNullOrWhiteSpace(login))
                    continue;


                var existingMember =
                    team.Members.FirstOrDefault(m =>
                        string.Equals(
                            m.GitHubUsername?.Trim(),
                            login,
                            StringComparison.OrdinalIgnoreCase));


                if (existingMember != null)
                    continue;


                var newMember =
                    new TeamMember
                    {
                        TeamId =
                            team.TeamId,

                        GitHubUsername =
                            login,

                        AddedAt =
                            DateTime.UtcNow
                    };


                _context.TeamMembers.Add(newMember);
                team.Members.Add(newMember);

                addedCount++;


                _logger.LogInformation(
                    "Додано GitHub-учасника: {Login}",
                    login);
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"GitHub-проєкт {owner}/{repository} " +
                $"успішно підключено. " +
                $"Знайдено GitHub-користувачів: " +
                $"{githubUsers.Count}. " +
                $"Додано нових: {addedCount}.";

            return RedirectToAction(
                "Details",
                "Team",
                new { id = teamId });
        }


        // ============================================================
        // REFRESH TEAM MEMBERS FROM GITHUB
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshMembers(
            int teamId)
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
                .Include(t => t.Members)
                .FirstOrDefaultAsync();

            if (team == null)
                return NotFound();


            if (string.IsNullOrWhiteSpace(team.GitHubOwner) ||
                string.IsNullOrWhiteSpace(team.GitHubRepository))
            {
                TempData["Error"] =
                    "До команди ще не підключено GitHub-проєкт.";

                return RedirectToAction(
                    "Details",
                    "Team",
                    new { id = teamId });
            }


            var owner =
                team.GitHubOwner;

            var repository =
                team.GitHubRepository;


            // ========================================================
            // COLLABORATORS
            // ========================================================

            var collaborators =
                await _gitHubService
                    .GetRepositoryCollaboratorsAsync(
                        owner,
                        repository);


            // ========================================================
            // CONTRIBUTORS
            // ========================================================

            var contributors =
                await _gitHubService
                    .GetRepositoryContributorsAsync(
                        owner,
                        repository);


            var githubUsers =
                collaborators
                    .Concat(contributors)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Login))
                    .GroupBy(
                        x => x.Login,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();


            int addedCount = 0;


            foreach (var githubUser in githubUsers)
            {
                var login =
                    githubUser.Login?.Trim();

                if (string.IsNullOrWhiteSpace(login))
                    continue;


                var existingMember =
                    team.Members.FirstOrDefault(m =>
                        string.Equals(
                            m.GitHubUsername?.Trim(),
                            login,
                            StringComparison.OrdinalIgnoreCase));


                if (existingMember != null)
                    continue;


                var newMember =
                    new TeamMember
                    {
                        TeamId =
                            team.TeamId,

                        GitHubUsername =
                            login,

                        AddedAt =
                            DateTime.UtcNow
                    };


                _context.TeamMembers.Add(newMember);
                team.Members.Add(newMember);

                addedCount++;


                _logger.LogInformation(
                    "RefreshMembers: додано {Login}",
                    login);
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Список учасників оновлено. " +
                $"GitHub повернув {githubUsers.Count} користувачів. " +
                $"Додано нових: {addedCount}.";

            return RedirectToAction(
                "Details",
                "Team",
                new { id = teamId });
        }


        // ============================================================
        // SYNC GITHUB ACTIVITY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sync(int id)
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
                .FirstOrDefaultAsync();

            if (team == null)
                return NotFound();


            if (string.IsNullOrWhiteSpace(team.GitHubOwner) ||
                string.IsNullOrWhiteSpace(team.GitHubRepository))
            {
                TempData["Error"] =
                    "До команди не підключено GitHub-проєкт.";

                return RedirectToAction(
                    nameof(Team),
                    new { id });
            }


            var owner =
                team.GitHubOwner;

            var repository =
                team.GitHubRepository;


            // ========================================================
            // ПЕРІОД
            // ========================================================

            var periodEnd =
                DateTime.UtcNow;

            var periodStart =
                periodEnd.AddDays(-7);


            _logger.LogInformation(
                "================================================");

            _logger.LogInformation(
                "ПОЧАТОК GITHUB SYNC");

            _logger.LogInformation(
                "Repository: {Owner}/{Repository}",
                owner,
                repository);

            _logger.LogInformation(
                "Поточних учасників: {Count}",
                team.Members.Count);


            // ========================================================
            // 1. COLLABORATORS
            // ========================================================

            var collaborators =
                await _gitHubService
                    .GetRepositoryCollaboratorsAsync(
                        owner,
                        repository);


            _logger.LogInformation(
                "GitHub collaborators: {Count}",
                collaborators.Count);


            // ========================================================
            // 2. CONTRIBUTORS
            // ========================================================

            var contributors =
                await _gitHubService
                    .GetRepositoryContributorsAsync(
                        owner,
                        repository);


            _logger.LogInformation(
                "GitHub contributors: {Count}",
                contributors.Count);


            // ========================================================
            // ДОДАЄМО ВІДОМИХ GITHUB-КОРИСТУВАЧІВ
            // ========================================================

            var githubUsers =
                collaborators
                    .Concat(contributors)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Login))
                    .GroupBy(
                        x => x.Login,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();


            foreach (var githubUser in githubUsers)
            {
                _logger.LogInformation(
                    "GitHub user: {Login}",
                    githubUser.Login);
            }


            int githubMembersAdded = 0;


            foreach (var githubUser in githubUsers)
            {
                var login =
                    githubUser.Login?.Trim();

                if (string.IsNullOrWhiteSpace(login))
                    continue;


                var existingMember =
                    team.Members.FirstOrDefault(m =>
                        string.Equals(
                            m.GitHubUsername?.Trim(),
                            login,
                            StringComparison.OrdinalIgnoreCase));


                if (existingMember != null)
                    continue;


                var newMember =
                    new TeamMember
                    {
                        TeamId =
                            team.TeamId,

                        GitHubUsername =
                            login,

                        AddedAt =
                            DateTime.UtcNow
                    };


                _context.TeamMembers.Add(newMember);
                team.Members.Add(newMember);

                githubMembersAdded++;


                _logger.LogInformation(
                    "Автоматично додано GitHub-користувача: {Login}",
                    login);
            }


            // ========================================================
            // 3. COMMITS
            // ========================================================

            var commits =
                await _gitHubService
                    .GetRepositoryCommitsAsync(
                        owner,
                        repository,
                        periodStart,
                        periodEnd);


            _logger.LogInformation(
                "GitHub commits: {Count}",
                commits.Count);


            // ========================================================
            // 4. PULL REQUESTS
            // ========================================================

            var pullRequests =
                await _gitHubService
                    .GetPullRequestsAsync(
                        owner,
                        repository);


            _logger.LogInformation(
                "GitHub pull requests: {Count}",
                pullRequests.Count);


            // ========================================================
            // 5. ISSUES
            // ========================================================

            var issues =
                await _gitHubService
                    .GetRepositoryIssuesAsync(
                        owner,
                        repository);


            _logger.LogInformation(
                "GitHub issues: {Count}",
                issues.Count);


            // ========================================================
            // 6. REVIEWS
            // ========================================================

            var allReviews =
                new List<GitHubReview>();


            foreach (var pullRequest in pullRequests)
            {
                var reviews =
                    await _gitHubService
                        .GetPullRequestReviewsAsync(
                            owner,
                            repository,
                            pullRequest.Number);

                allReviews.AddRange(reviews);
            }


            _logger.LogInformation(
                "GitHub reviews: {Count}",
                allReviews.Count);


            // ========================================================
            // ДІАГНОСТИКА COMMIT
            // ========================================================

            _logger.LogInformation(
                "================================================");

            _logger.LogInformation(
                "GITHUB COMMITS");

            foreach (var commit in commits)
            {
                _logger.LogInformation(
                    "------------------------------------------------");

                _logger.LogInformation(
                    "SHA: {Sha}",
                    commit.Sha);

                _logger.LogInformation(
                    "Author login: {Login}",
                    commit.Author?.Login);

                _logger.LogInformation(
                    "Committer login: {Login}",
                    commit.Committer?.Login);

                _logger.LogInformation(
                    "Author name: {Name}",
                    commit.Commit?.Author?.Name);

                _logger.LogInformation(
                    "Author email: {Email}",
                    commit.Commit?.Author?.Email);

                _logger.LogInformation(
                    "Committer name: {Name}",
                    commit.Commit?.Committer?.Name);

                _logger.LogInformation(
                    "Committer email: {Email}",
                    commit.Commit?.Committer?.Email);
            }


            // ========================================================
            // ВИЯВЛЕННЯ АВТОРІВ COMMIT
            // ========================================================

            var commitIdentities =
                new List<CommitIdentity>();


            foreach (var commit in commits)
            {
                var authorLogin =
                    commit.Author?.Login?.Trim();

                var committerLogin =
                    commit.Committer?.Login?.Trim();

                var authorName =
                    commit.Commit?.Author?.Name?.Trim();

                var authorEmail =
                    commit.Commit?.Author?.Email?.Trim();

                var committerName =
                    commit.Commit?.Committer?.Name?.Trim();

                var committerEmail =
                    commit.Commit?.Committer?.Email?.Trim();


                // ----------------------------------------------------
                // Автор commit
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(authorLogin) ||
                    !string.IsNullOrWhiteSpace(authorEmail) ||
                    !string.IsNullOrWhiteSpace(authorName))
                {
                    AddCommitIdentity(
                        commitIdentities,
                        authorLogin,
                        authorName,
                        authorEmail);
                }


                // ----------------------------------------------------
                // Committer
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(committerLogin) ||
                    !string.IsNullOrWhiteSpace(committerEmail) ||
                    !string.IsNullOrWhiteSpace(committerName))
                {
                    AddCommitIdentity(
                        commitIdentities,
                        committerLogin,
                        committerName,
                        committerEmail);
                }
            }


            _logger.LogInformation(
                "Унікальних commit identities: {Count}",
                commitIdentities.Count);


            foreach (var identity in commitIdentities)
            {
                _logger.LogInformation(
                    "Commit identity: Login={Login}, Name={Name}, Email={Email}",
                    identity.Login,
                    identity.Name,
                    identity.Email);
            }


            // ========================================================
            // ПРИВ'ЯЗУЄМО COMMIT IDENTITY ДО TEAM MEMBER
            // ========================================================

            int identityMembersAdded = 0;


            foreach (var identity in commitIdentities)
            {
                var member =
                    FindMemberForCommitIdentity(
                        team.Members,
                        identity);


                if (member != null)
                {
                    // ------------------------------------------------
                    // Якщо GitHub тепер повернув справжній Login,
                    // оновлюємо існуючого учасника.
                    // ------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(identity.Login) &&
                        !string.Equals(
                            member.GitHubUsername,
                            identity.Login,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation(
                            "Оновлення GitHub username: {Old} -> {New}",
                            member.GitHubUsername,
                            identity.Login);

                        member.GitHubUsername =
                            identity.Login;
                    }


                    if (!string.IsNullOrWhiteSpace(identity.Email) &&
                        string.IsNullOrWhiteSpace(member.GitHubEmail))
                    {
                        member.GitHubEmail =
                            identity.Email;
                    }


                    continue;
                }


                // ----------------------------------------------------
                // Якщо login відсутній, але є ім'я/email,
                // створюємо локальну GitHub identity.
                //
                // GitHubUsername тут використовується як ключ
                // ідентифікації, доки GitHub не поверне справжній login.
                // ----------------------------------------------------

                var fallbackUsername =
                    !string.IsNullOrWhiteSpace(identity.Login)
                        ? identity.Login
                        : identity.Name;


                if (string.IsNullOrWhiteSpace(fallbackUsername))
                {
                    _logger.LogWarning(
                        "Неможливо створити TeamMember: " +
                        "відсутні Login та Name.");

                    continue;
                }


                var newMember =
                    new TeamMember
                    {
                        TeamId =
                            team.TeamId,

                        GitHubUsername =
                            fallbackUsername,

                        GitHubEmail =
                            string.IsNullOrWhiteSpace(identity.Email)
                                ? null
                                : identity.Email,

                        AddedAt =
                            DateTime.UtcNow
                    };


                _context.TeamMembers.Add(newMember);
                team.Members.Add(newMember);

                identityMembersAdded++;


                _logger.LogInformation(
                    "Створено учасника на основі commit identity: " +
                    "Username={Username}, Email={Email}",
                    fallbackUsername,
                    identity.Email);
            }


            if (githubMembersAdded > 0 ||
                identityMembersAdded > 0)
            {
                await _context.SaveChangesAsync();
            }


            // ========================================================
            // РОЗРАХУНОК АКТИВНОСТІ
            // ========================================================

            _logger.LogInformation(
                "================================================");

            _logger.LogInformation(
                "ПОЧАТОК РОЗРАХУНКУ АКТИВНОСТІ");

            _logger.LogInformation(
                "Учасників після автоматичного визначення: {Count}",
                team.Members.Count);


            foreach (var member in team.Members)
            {
                int commitsCount = 0;
                int pullRequestsCount = 0;
                int issuesCount = 0;
                int reviewsCount = 0;


                // ====================================================
                // COMMITS
                // ====================================================

                foreach (var commit in commits)
                {
                    if (IsCommitBelongsToMember(
                        commit,
                        member))
                    {
                        commitsCount++;


                        if (string.IsNullOrWhiteSpace(
                                member.GitHubEmail))
                        {
                            var email =
                                GetCommitEmail(commit);

                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                member.GitHubEmail =
                                    email;
                            }
                        }
                    }
                }


                // ====================================================
                // PULL REQUESTS
                // ====================================================

                pullRequestsCount =
                    pullRequests.Count(pr =>
                        pr.User != null &&
                        IsSameGitHubUser(
                            pr.User,
                            member.GitHubUsername) &&
                        pr.CreatedAt >= periodStart &&
                        pr.CreatedAt <= periodEnd);


                // ====================================================
                // ISSUES
                // ====================================================

                issuesCount =
                    issues.Count(issue =>
                        issue.User != null &&
                        IsSameGitHubUser(
                            issue.User,
                            member.GitHubUsername) &&
                        issue.CreatedAt >= periodStart &&
                        issue.CreatedAt <= periodEnd &&
                        issue.PullRequest == null);


                // ====================================================
                // REVIEWS
                // ====================================================

                reviewsCount =
                    allReviews.Count(review =>
                        review.User != null &&
                        IsSameGitHubUser(
                            review.User,
                            member.GitHubUsername) &&
                        review.SubmittedAt.HasValue &&
                        review.SubmittedAt.Value >= periodStart &&
                        review.SubmittedAt.Value <= periodEnd);


                // ====================================================
                // ЛОГ
                // ====================================================

                _logger.LogInformation(
                    "Учасник: {Username}",
                    member.GitHubUsername);

                _logger.LogInformation(
                    "Email: {Email}",
                    member.GitHubEmail);

                _logger.LogInformation(
                    "Commits: {Commits}",
                    commitsCount);

                _logger.LogInformation(
                    "Pull Requests: {PullRequests}",
                    pullRequestsCount);

                _logger.LogInformation(
                    "Issues: {Issues}",
                    issuesCount);

                _logger.LogInformation(
                    "Reviews: {Reviews}",
                    reviewsCount);


                // ====================================================
                // ЗАПИС АКТИВНОСТІ
                // ====================================================

                var activity =
                    new GitHubActivity
                    {
                        TeamMemberId =
                            member.TeamMemberId,

                        GitHubUsername =
                            member.GitHubUsername,

                        ActivityDate =
                            DateTime.UtcNow,

                        CommitsCount =
                            commitsCount,

                        PullRequestsCount =
                            pullRequestsCount,

                        IssuesCount =
                            issuesCount,

                        ReviewsCount =
                            reviewsCount,

                        PeriodStart =
                            periodStart,

                        PeriodEnd =
                            periodEnd,

                        CreatedAt =
                            DateTime.UtcNow
                    };


                _context.GitHubActivities.Add(activity);
            }


            await _context.SaveChangesAsync();


            // ========================================================
            // ЗАВЕРШЕННЯ
            // ========================================================

            _logger.LogInformation(
                "================================================");

            _logger.LogInformation(
                "GITHUB SYNC ЗАВЕРШЕНО");

            _logger.LogInformation(
                "Repository: {Owner}/{Repository}",
                owner,
                repository);

            _logger.LogInformation(
                "Commits: {Commits}",
                commits.Count);

            _logger.LogInformation(
                "Pull Requests: {PullRequests}",
                pullRequests.Count);

            _logger.LogInformation(
                "Issues: {Issues}",
                issues.Count);

            _logger.LogInformation(
                "Reviews: {Reviews}",
                allReviews.Count);

            _logger.LogInformation(
                "GitHub users added: {Count}",
                githubMembersAdded);

            _logger.LogInformation(
                "Commit identities added: {Count}",
                identityMembersAdded);

            _logger.LogInformation(
                "Total team members: {Count}",
                team.Members.Count);

            _logger.LogInformation(
                "================================================");


            TempData["Success"] =
                $"GitHub-активність проєкту " +
                $"{owner}/{repository} успішно оновлено. " +
                $"Оброблено комітів: {commits.Count}. " +
                $"Учасників: {team.Members.Count}.";

            return RedirectToAction(
                nameof(Team),
                new { id });
        }


        // ============================================================
        // ДОДАВАННЯ COMMIT IDENTITY
        // ============================================================

        private void AddCommitIdentity(
            List<CommitIdentity> identities,
            string? login,
            string? name,
            string? email)
        {
            login =
                string.IsNullOrWhiteSpace(login)
                    ? null
                    : login.Trim();

            name =
                string.IsNullOrWhiteSpace(name)
                    ? null
                    : name.Trim();

            email =
                string.IsNullOrWhiteSpace(email)
                    ? null
                    : email.Trim();


            var existing =
                identities.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(email) &&
                    !string.IsNullOrWhiteSpace(x.Email) &&
                    string.Equals(
                        x.Email,
                        email,
                        StringComparison.OrdinalIgnoreCase));


            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.Login) &&
                    !string.IsNullOrWhiteSpace(login))
                {
                    existing.Login = login;
                }

                if (string.IsNullOrWhiteSpace(existing.Name) &&
                    !string.IsNullOrWhiteSpace(name))
                {
                    existing.Name = name;
                }

                return;
            }


            if (!string.IsNullOrWhiteSpace(login))
            {
                existing =
                    identities.FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.Login) &&
                        string.Equals(
                            x.Login,
                            login,
                            StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    if (string.IsNullOrWhiteSpace(existing.Email) &&
                        !string.IsNullOrWhiteSpace(email))
                    {
                        existing.Email = email;
                    }

                    if (string.IsNullOrWhiteSpace(existing.Name) &&
                        !string.IsNullOrWhiteSpace(name))
                    {
                        existing.Name = name;
                    }

                    return;
                }
            }


            identities.Add(
                new CommitIdentity
                {
                    Login = login,
                    Name = name,
                    Email = email
                });
        }


        // ============================================================
        // ПОШУК УЧАСНИКА ДЛЯ COMMIT
        // ============================================================

        private TeamMember? FindMemberForCommitIdentity(
            ICollection<TeamMember> members,
            CommitIdentity identity)
        {
            // --------------------------------------------------------
            // 1. Найточніше — GitHub Login
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(identity.Login))
            {
                var byLogin =
                    members.FirstOrDefault(m =>
                        string.Equals(
                            m.GitHubUsername?.Trim(),
                            identity.Login.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (byLogin != null)
                    return byLogin;
            }


            // --------------------------------------------------------
            // 2. Другий варіант — email
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(identity.Email))
            {
                var byEmail =
                    members.FirstOrDefault(m =>
                        !string.IsNullOrWhiteSpace(m.GitHubEmail) &&
                        string.Equals(
                            m.GitHubEmail.Trim(),
                            identity.Email.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (byEmail != null)
                    return byEmail;
            }


            // --------------------------------------------------------
            // 3. Якщо login немає, але ім'я збігається з username
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(identity.Name))
            {
                var byName =
                    members.FirstOrDefault(m =>
                        string.Equals(
                            m.GitHubUsername?.Trim(),
                            identity.Name.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (byName != null)
                    return byName;
            }


            return null;
        }


        // ============================================================
        // ПЕРЕВІРКА COMMIT
        // ============================================================

        private bool IsCommitBelongsToMember(
            GitHubCommit commit,
            TeamMember member)
        {
            if (string.IsNullOrWhiteSpace(
                member.GitHubUsername))
            {
                return false;
            }


            // --------------------------------------------------------
            // 1. Author Login
            // --------------------------------------------------------

            if (IsSameGitHubUser(
                commit.Author,
                member.GitHubUsername))
            {
                return true;
            }


            // --------------------------------------------------------
            // 2. Committer Login
            // --------------------------------------------------------

            if (IsSameGitHubUser(
                commit.Committer,
                member.GitHubUsername))
            {
                return true;
            }


            // --------------------------------------------------------
            // 3. Author Email
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    member.GitHubEmail) &&
                !string.IsNullOrWhiteSpace(
                    commit.Commit?.Author?.Email))
            {
                if (string.Equals(
                    member.GitHubEmail.Trim(),
                    commit.Commit.Author.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            // --------------------------------------------------------
            // 4. Committer Email
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    member.GitHubEmail) &&
                !string.IsNullOrWhiteSpace(
                    commit.Commit?.Committer?.Email))
            {
                if (string.Equals(
                    member.GitHubEmail.Trim(),
                    commit.Commit.Committer.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            // --------------------------------------------------------
            // 5. Якщо GitHub Login відсутній,
            //    використовуємо ім'я автора як fallback.
            //
            // Це потрібно для випадку:
            //
            // Author.Login = null
            // Author.Name  = uamax
            //
            // TeamMember.GitHubUsername = uamax
            // --------------------------------------------------------

            var authorName =
                commit.Commit?.Author?.Name;

            if (!string.IsNullOrWhiteSpace(authorName) &&
                string.Equals(
                    authorName.Trim(),
                    member.GitHubUsername.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            var committerName =
                commit.Commit?.Committer?.Name;

            if (!string.IsNullOrWhiteSpace(committerName) &&
                string.Equals(
                    committerName.Trim(),
                    member.GitHubUsername.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            return false;
        }


        // ============================================================
        // ПОРІВНЯННЯ GITHUB USERS
        // ============================================================

        private bool IsSameGitHubUser(
            GitHubApiUser? githubUser,
            string username)
        {
            if (githubUser == null)
                return false;

            if (string.IsNullOrWhiteSpace(
                githubUser.Login))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                username))
            {
                return false;
            }

            return string.Equals(
                githubUser.Login.Trim(),
                username.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }


        // ============================================================
        // EMAIL З COMMIT
        // ============================================================

        private string? GetCommitEmail(
            GitHubCommit commit)
        {
            if (!string.IsNullOrWhiteSpace(
                commit.Commit?.Author?.Email))
            {
                return commit.Commit.Author.Email.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                commit.Commit?.Committer?.Email))
            {
                return commit.Commit.Committer.Email.Trim();
            }

            return null;
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


        // ============================================================
        // ВНУТРІШНЯ МОДЕЛЬ ІДЕНТИФІКАЦІЇ COMMIT
        // ============================================================

        private class CommitIdentity
        {
            public string? Login { get; set; }

            public string? Name { get; set; }

            public string? Email { get; set; }
        }
    }
}