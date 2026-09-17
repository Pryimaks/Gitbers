using System.Net.Http.Json;
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

        // ==========================================
        // Користувач
        // ==========================================

        public async Task<GitHubUser?> GetUserAsync(string username)
        {
            var response = await _httpClient.GetAsync(
                $"users/{Uri.EscapeDataString(username)}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<GitHubUser>();
        }


        // ==========================================
        // Репозиторії користувача
        // ==========================================

        public async Task<List<GitHubRepository>> GetRepositoriesAsync(
            string username)
        {
            var response = await _httpClient.GetAsync(
                $"users/{Uri.EscapeDataString(username)}/repos?per_page=100");

            if (!response.IsSuccessStatusCode)
                return new List<GitHubRepository>();

            return await response.Content
                .ReadFromJsonAsync<List<GitHubRepository>>()
                ?? new List<GitHubRepository>();
        }


        // ==========================================
        // Репозиторії організації
        // ==========================================

        public async Task<List<GitHubRepository>>
            GetOrganizationRepositoriesAsync(string organization)
        {
            var response = await _httpClient.GetAsync(
                $"orgs/{Uri.EscapeDataString(organization)}/repos?per_page=100");

            if (!response.IsSuccessStatusCode)
                return new List<GitHubRepository>();

            return await response.Content
                .ReadFromJsonAsync<List<GitHubRepository>>()
                ?? new List<GitHubRepository>();
        }


        // ==========================================
        // Коміти
        // ==========================================

        public async Task<List<GitHubCommit>> GetCommitsAsync(
            string owner,
            string repository,
            string username,
            DateTime since,
            DateTime until)
        {
            var sinceUtc = since
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ssZ");

            var untilUtc = until
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ssZ");

            var url =
                $"repos/{Uri.EscapeDataString(owner)}/" +
                $"{Uri.EscapeDataString(repository)}/commits" +
                $"?author={Uri.EscapeDataString(username)}" +
                $"&since={Uri.EscapeDataString(sinceUtc)}" +
                $"&until={Uri.EscapeDataString(untilUtc)}" +
                $"&per_page=100";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<GitHubCommit>();

            return await response.Content
                .ReadFromJsonAsync<List<GitHubCommit>>()
                ?? new List<GitHubCommit>();
        }


        // ==========================================
        // Pull Requests
        // ==========================================

        public async Task<List<GitHubPullRequest>>
            GetPullRequestsAsync(
                string owner,
                string repository)
        {
            var url =
                $"repos/{Uri.EscapeDataString(owner)}/" +
                $"{Uri.EscapeDataString(repository)}/pulls" +
                $"?state=all&per_page=100";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<GitHubPullRequest>();

            return await response.Content
                .ReadFromJsonAsync<List<GitHubPullRequest>>()
                ?? new List<GitHubPullRequest>();
        }


        // ==========================================
        // Issues
        // ==========================================

        public async Task<List<GitHubIssue>>
            GetIssuesAsync(
                string owner,
                string repository,
                string username)
        {
            var url =
                $"repos/{Uri.EscapeDataString(owner)}/" +
                $"{Uri.EscapeDataString(repository)}/issues" +
                $"?creator={Uri.EscapeDataString(username)}" +
                $"&state=all&per_page=100";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<GitHubIssue>();

            return await response.Content
                .ReadFromJsonAsync<List<GitHubIssue>>()
                ?? new List<GitHubIssue>();
        }


        // ==========================================
        // Reviews Pull Request
        // ==========================================

        public async Task<List<GitHubReview>>
            GetPullRequestReviewsAsync(
                string owner,
                string repository,
                int pullRequestNumber)
        {
            var url =
                $"repos/{Uri.EscapeDataString(owner)}/" +
                $"{Uri.EscapeDataString(repository)}/" +
                $"pulls/{pullRequestNumber}/reviews" +
                $"?per_page=100";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<GitHubReview>();

            return await response.Content
                .ReadFromJsonAsync<List<GitHubReview>>()
                ?? new List<GitHubReview>();
        }
    }


    // ==========================================
    // GitHub API models
    // ==========================================

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


    public class GitHubRepository
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("owner")]
        public GitHubRepositoryOwner? Owner { get; set; }
    }


    public class GitHubRepositoryOwner
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }


    public class GitHubCommit
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("commit")]
        public GitHubCommitInfo? Commit { get; set; }
    }


    public class GitHubCommitInfo
    {
        [JsonPropertyName("author")]
        public GitHubCommitAuthor? Author { get; set; }
    }


    public class GitHubCommitAuthor
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }
    }


    // ==========================================
    // Pull Request
    // ==========================================

    public class GitHubPullRequest
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("user")]
        public GitHubApiUser? User { get; set; }
    }


    // ==========================================
    // Issue
    // ==========================================

    public class GitHubIssue
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("user")]
        public GitHubApiUser? User { get; set; }

        // Якщо це Pull Request,
        // GitHub повертає цей об'єкт.
        [JsonPropertyName("pull_request")]
        public object? PullRequest { get; set; }
    }


    // ==========================================
    // Review
    // ==========================================

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


    // ==========================================
    // Спільна модель GitHub користувача
    // ==========================================

    public class GitHubApiUser
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }
}