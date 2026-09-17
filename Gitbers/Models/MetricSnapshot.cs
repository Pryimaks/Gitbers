using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class MetricSnapshot
    {
        [Key]
        public int SnapshotId { get; set; }

        public int TeamId { get; set; }

        public Team? Team { get; set; }

        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public double ActivityScore { get; set; }

        public double CollaborationScore { get; set; }

        public double StabilityScore { get; set; }

        public double RiskScore { get; set; }

        [MaxLength(50)]
        public string RiskLevel { get; set; } = "Low";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
