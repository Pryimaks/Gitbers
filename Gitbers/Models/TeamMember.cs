using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class TeamMember
    {
        [Key]
        public int TeamMemberId { get; set; }

        public int TeamId { get; set; }

        public Team? Team { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string GitHubUsername { get; set; } = string.Empty;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
