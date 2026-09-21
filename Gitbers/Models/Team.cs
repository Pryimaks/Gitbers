using Gitbers.Data;
using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class Team
    {
        public int TeamId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? GitHubOrganization { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int OwnerId { get; set; }

        public User? Owner { get; set; }
        // Учасники команди
        public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();

        // Знімки метрик
        public ICollection<MetricSnapshot> MetricSnapshots { get; set; } = new List<MetricSnapshot>();
    }
}