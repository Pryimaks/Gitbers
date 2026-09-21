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

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? ViberUserId { get; set; }

        public bool ViberConnected { get; set; } = false;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string? ViberConnectionCode { get; set; }

        public DateTime? ViberConnectionCodeExpiresAt { get; set; }
    }
}