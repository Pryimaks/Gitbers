using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class SurveyInvitation
    {
        [Key]
        public int SurveyInvitationId { get; set; }

        public int SurveyId { get; set; }
        public Survey? Survey { get; set; }

        public int TeamMemberId { get; set; }
        public TeamMember? TeamMember { get; set; }

        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }
    }
}