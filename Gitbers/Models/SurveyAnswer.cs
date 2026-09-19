using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class SurveyAnswer
    {
        [Key]
        public int SurveyAnswerId { get; set; }

        public int SurveyId { get; set; }

        public Survey? Survey { get; set; }

        // Номер питання від 1 до 5
        public int QuestionNumber { get; set; }

        // Оцінка від 1 до 5
        public int Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}