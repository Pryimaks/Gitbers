using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class GitHubActivity
    {
        [Key]
        public int ActivityId { get; set; }

        public int TeamMemberId { get; set; }

        public TeamMember? TeamMember { get; set; }

        [MaxLength(100)]
        public string GitHubUsername { get; set; } = string.Empty;

        public DateTime ActivityDate { get; set; }

        public int CommitsCount { get; set; }

        public int PullRequestsCount { get; set; }

        public int IssuesCount { get; set; }

        public int ReviewsCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
