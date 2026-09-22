using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gitbers.Services
{
    public class GitHubService
    {
        private readonly HttpClient _httpClient;

        public GitHubService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ============================================================
        // КОРИСТУВАЧ
        // ============================================================

        public async Task<GitHubUser?> GetUserAsync(
            string username)
        {
            var response = await _httpClient.GetAsync(
                $"users/{Uri.EscapeDataString(username)}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<GitHubUser>();
        }


        // ============================================================
        // РЕПОЗИТОРІЇ КОНКРЕТНОГО КОРИСТУВАЧА
        // ============================================================

        public async Task<List<GitHubRepository>>
            GetRepositoriesAsync(
                string username)
        {
            var repositories =
                new List<GitHubRepository>();

            int page = 1;

            while (true)
            {
                var url =
                    $"users/{Uri.EscapeDataString(username)}/repos" +
                    $"?per_page=100" +
                    $"&page={page}" +
                    $"&sort=updated";

                var response =
                    await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    break;

                var pageRepositories =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubRepository>>();

                if (pageRepositories == null ||
                    pageRepositories.Count == 0)
                {
                    break;
                }

                repositories.AddRange(
                    pageRepositories);

                if (pageRepositories.Count < 100)
                    break;

                page++;
            }

            return repositories;
        }


        // ============================================================
        // РЕПОЗИТОРІЇ АВТОРИЗОВАНОГО GITHUB-КОРИСТУВАЧА
        // ============================================================

        public async Task<List<GitHubRepository>>
            GetMyRepositoriesAsync()
        {
            var repositories =
                new List<GitHubRepository>();

            int page = 1;

            while (true)
            {
                var url =
                    $"user/repos" +
                    $"?per_page=100" +
                    $"&page={page}" +
                    $"&sort=updated" +
                    $"&affiliation=owner,collaborator,organization_member";

                var response =
                    await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync();

                    throw new HttpRequestException(
                        $"GitHub API повернув помилку " +
                        $"{(int)response.StatusCode} " +
                        $"{response.ReasonPhrase}. " +
                        $"Відповідь: {error}");
                }

                var pageRepositories =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubRepository>>();

                if (pageRepositories == null ||
                    pageRepositories.Count == 0)
                {
                    break;
                }

                repositories.AddRange(
                    pageRepositories);

                if (pageRepositories.Count < 100)
                    break;

                page++;
            }

            return repositories;
        }


        // ============================================================
        // РЕПОЗИТОРІЇ ОРГАНІЗАЦІЇ
        // ============================================================

        public async Task<List<GitHubRepository>>
            GetOrganizationRepositoriesAsync(
                string organization)
        {
            var repositories =
                new List<GitHubRepository>();

            int page = 1;

            while (true)
            {
                var response =
                    await _httpClient.GetAsync(
                        $"orgs/{Uri.EscapeDataString(organization)}/repos" +
                        $"?per_page=100" +
                        $"&page={page}");

                if (!response.IsSuccessStatusCode)
                    break;

                var pageRepositories =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubRepository>>();

                if (pageRepositories == null ||
                    pageRepositories.Count == 0)
                {
                    break;
                }

                repositories.AddRange(
                    pageRepositories);

                if (pageRepositories.Count < 100)
                    break;

                page++;
            }

            return repositories;
        }


        // ============================================================
        // ІНФОРМАЦІЯ ПРО КОНКРЕТНИЙ РЕПОЗИТОРІЙ
        // ============================================================

        public async Task<GitHubRepository?>
            GetRepositoryAsync(
                string owner,
                string repository)
        {
            var response =
                await _httpClient.GetAsync(
                    $"repos/{Uri.EscapeDataString(owner)}/" +
                    $"{Uri.EscapeDataString(repository)}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<GitHubRepository>();
        }


        // ============================================================
        // COLLABORATORS РЕПОЗИТОРІЮ
        // ============================================================

        public async Task<List<GitHubCollaborator>>
            GetRepositoryCollaboratorsAsync(
                string owner,
                string repository)
        {
            var collaborators =
                new List<GitHubCollaborator>();

            int page = 1;

            while (true)
            {
                var response =
                    await _httpClient.GetAsync(
                        $"repos/{Uri.EscapeDataString(owner)}/" +
                        $"{Uri.EscapeDataString(repository)}/" +
                        $"collaborators" +
                        $"?per_page=100" +
                        $"&page={page}");

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync();

                    throw new HttpRequestException(
                        $"Не вдалося отримати учасників GitHub-репозиторію. " +
                        $"Код: {(int)response.StatusCode}. " +
                        $"Відповідь: {error}");
                }

                var pageCollaborators =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubCollaborator>>();

                if (pageCollaborators == null ||
                    pageCollaborators.Count == 0)
                {
                    break;
                }

                collaborators.AddRange(
                    pageCollaborators);

                if (pageCollaborators.Count < 100)
                    break;

                page++;
            }

            return collaborators;
        }


        // ============================================================
        // CONTRIBUTORS РЕПОЗИТОРІЮ
        // ============================================================

        public async Task<List<GitHubCollaborator>>
            GetRepositoryContributorsAsync(
                string owner,
                string repository)
        {
            var contributors =
                new List<GitHubCollaborator>();

            int page = 1;

            while (true)
            {
                var url =
                    $"repos/{Uri.EscapeDataString(owner)}/" +
                    $"{Uri.EscapeDataString(repository)}/" +
                    $"contributors" +
                    $"?per_page=100" +
                    $"&page={page}";

                var response =
                    await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync();

                    throw new HttpRequestException(
                        $"Не вдалося отримати contributors GitHub-репозиторію. " +
                        $"Код: {(int)response.StatusCode}. " +
                        $"Відповідь: {error}");
                }

                var pageContributors =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubCollaborator>>();

                if (pageContributors == null ||
                    pageContributors.Count == 0)
                {
                    break;
                }

                contributors.AddRange(
                    pageContributors);

                if (pageContributors.Count < 100)
                    break;

                page++;
            }

            return contributors;
        }


        // ============================================================
        // ВСІ КОМІТИ РЕПОЗИТОРІЮ ЗА ПЕРІОД
        // ============================================================

        public async Task<List<GitHubCommit>>
            GetRepositoryCommitsAsync(
                string owner,
                string repository,
                DateTime since,
                DateTime until)
        {
            var commits =
                new List<GitHubCommit>();

            var sinceUtc =
                since.ToUniversalTime()
                    .ToString(
                        "yyyy-MM-ddTHH:mm:ssZ");

            var untilUtc =
                until.ToUniversalTime()
                    .ToString(
                        "yyyy-MM-ddTHH:mm:ssZ");

            int page = 1;

            while (true)
            {
                var url =
                    $"repos/{Uri.EscapeDataString(owner)}/" +
                    $"{Uri.EscapeDataString(repository)}/commits" +
                    $"?since={Uri.EscapeDataString(sinceUtc)}" +
                    $"&until={Uri.EscapeDataString(untilUtc)}" +
                    $"&per_page=100" +
                    $"&page={page}";

                var response =
                    await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync();

                    throw new HttpRequestException(
                        $"Не вдалося отримати коміти GitHub. " +
                        $"Код: {(int)response.StatusCode}. " +
                        $"Відповідь: {error}");
                }

                var pageCommits =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubCommit>>();

                if (pageCommits == null ||
                    pageCommits.Count == 0)
                {
                    break;
                }

                commits.AddRange(
                    pageCommits);

                if (pageCommits.Count < 100)
                    break;

                page++;
            }

            return commits;
        }


        // ============================================================
        // PULL REQUESTS
        // ============================================================

        public async Task<List<GitHubPullRequest>>
            GetPullRequestsAsync(
                string owner,
                string repository)
        {
            var pullRequests =
                new List<GitHubPullRequest>();

            int page = 1;

            while (true)
            {
                var response =
                    await _httpClient.GetAsync(
                        $"repos/{Uri.EscapeDataString(owner)}/" +
                        $"{Uri.EscapeDataString(repository)}/pulls" +
                        $"?state=all" +
                        $"&per_page=100" +
                        $"&page={page}");

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync();

                    throw new HttpRequestException(
                        $"Не вдалося отримати Pull Requests GitHub. " +
                        $"Код: {(int)response.StatusCode}. " +
                        $"Відповідь: {error}");
                }

                var pagePullRequests =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubPullRequest>>();

                if (pagePullRequests == null ||
                    pagePullRequests.Count == 0)
                {
                    break;
                }

                pullRequests.AddRange(
                    pagePullRequests);

                if (pagePullRequests.Count < 100)
                    break;

                page++;
            }

            return pullRequests;
        }


        // ============================================================
        // ВСІ ISSUES РЕПОЗИТОРІЮ
        // ============================================================

        public async Task<List<GitHubIssue>>
            GetRepositoryIssuesAsync(
                string owner,
                string repository)
        {
            var issues =
                new List<GitHubIssue>();

            int page = 1;

            while (true)
            {
                var response =
                    await _httpClient.GetAsync(
                        $"repos/{Uri.EscapeDataString(owner)}/" +
                        $"{Uri.EscapeDataString(repository)}/issues" +
                        $"?state=all" +
                        $"&per_page=100" +
                        $"&page={page}");

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync();

                    throw new HttpRequestException(
                        $"Не вдалося отримати Issues GitHub. " +
                        $"Код: {(int)response.StatusCode}. " +
                        $"Відповідь: {error}");
                }

                var pageIssues =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubIssue>>();

                if (pageIssues == null ||
                    pageIssues.Count == 0)
                {
                    break;
                }

                issues.AddRange(
                    pageIssues);

                if (pageIssues.Count < 100)
                    break;

                page++;
            }

            return issues;
        }


        // ============================================================
        // REVIEWS PULL REQUEST
        // ============================================================

        public async Task<List<GitHubReview>>
            GetPullRequestReviewsAsync(
                string owner,
                string repository,
                int pullRequestNumber)
        {
            var reviews =
                new List<GitHubReview>();

            int page = 1;

            while (true)
            {
                var response =
                    await _httpClient.GetAsync(
                        $"repos/{Uri.EscapeDataString(owner)}/" +
                        $"{Uri.EscapeDataString(repository)}/" +
                        $"pulls/{pullRequestNumber}/reviews" +
                        $"?per_page=100" +
                        $"&page={page}");

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync();

                    throw new HttpRequestException(
                        $"Не вдалося отримати Reviews GitHub. " +
                        $"Код: {(int)response.StatusCode}. " +
                        $"Відповідь: {error}");
                }

                var pageReviews =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<GitHubReview>>();

                if (pageReviews == null ||
                    pageReviews.Count == 0)
                {
                    break;
                }

                reviews.AddRange(
                    pageReviews);

                if (pageReviews.Count < 100)
                    break;

                page++;
            }

            return reviews;
        }
    }


    // ================================================================
    // GITHUB USER
    // ================================================================

    public class GitHubUser
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("public_repos")]
        public int PublicRepositories { get; set; }

        [JsonPropertyName("followers")]
        public int Followers { get; set; }

        [JsonPropertyName("following")]
        public int Following { get; set; }
    }


    // ================================================================
    // GITHUB REPOSITORY
    // ================================================================

    public class GitHubRepository
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("private")]
        public bool Private { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        [JsonPropertyName("owner")]
        public GitHubRepositoryOwner? Owner { get; set; }
    }


    // ================================================================
    // GITHUB REPOSITORY OWNER
    // ================================================================

    public class GitHubRepositoryOwner
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }


    // ================================================================
    // GITHUB COLLABORATOR / CONTRIBUTOR
    // ================================================================

    public class GitHubCollaborator
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("contributions")]
        public int Contributions { get; set; }

        [JsonPropertyName("permissions")]
        public GitHubPermissions? Permissions { get; set; }
    }


    public class GitHubPermissions
    {
        [JsonPropertyName("admin")]
        public bool Admin { get; set; }

        [JsonPropertyName("push")]
        public bool Push { get; set; }

        [JsonPropertyName("pull")]
        public bool Pull { get; set; }
    }


    // ================================================================
    // COMMIT
    // ================================================================

    public class GitHubCommit
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public GitHubApiUser? Author { get; set; }

        [JsonPropertyName("committer")]
        public GitHubApiUser? Committer { get; set; }

        [JsonPropertyName("commit")]
        public GitHubCommitInfo? Commit { get; set; }
    }


    public class GitHubCommitInfo
    {
        [JsonPropertyName("author")]
        public GitHubCommitAuthor? Author { get; set; }

        [JsonPropertyName("committer")]
        public GitHubCommitAuthor? Committer { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }


    public class GitHubCommitAuthor
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }
    }


    // ================================================================
    // PULL REQUEST
    // ================================================================

    public class GitHubPullRequest
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("user")]
        public GitHubApiUser? User { get; set; }
    }


    // ================================================================
    // ISSUE
    // ================================================================

    public class GitHubIssue
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("user")]
        public GitHubApiUser? User { get; set; }

        [JsonPropertyName("pull_request")]
        public object? PullRequest { get; set; }
    }


    // ================================================================
    // REVIEW
    // ================================================================

    public class GitHubReview
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("user")]
        public GitHubApiUser? User { get; set; }

        [JsonPropertyName("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }
    }


    // ================================================================
    // API USER
    // ================================================================

    public class GitHubApiUser
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }
}