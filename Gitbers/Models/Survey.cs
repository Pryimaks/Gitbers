using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class Survey
    {
        [Key]
        public int SurveyId { get; set; }

        public int TeamId { get; set; }

        public Team? Team { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<SurveyAnswer> Answers { get; set; }
            = new List<SurveyAnswer>();
    }
}