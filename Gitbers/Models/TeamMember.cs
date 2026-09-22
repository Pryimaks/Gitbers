using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class TeamMember
    {
        [Key]
        public int TeamMemberId { get; set; }

        public int TeamId { get; set; }

        public Team? Team { get; set; }

        // Користувач Gitbers може бути відсутній.
        // GitHub-учасник може ще не мати акаунта в системі.
        public int? UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string GitHubUsername { get; set; } = string.Empty;

        // Email, який використовується у GitHub-комітах.
        // Може бути відсутнім, якщо GitHub його не повертає.
        [MaxLength(200)]
        public string? GitHubEmail { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}