using Gitbers.Data;
using Gitbers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
    public class SurveyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SurveyController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // Форма опитування
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Team)
                .FirstOrDefaultAsync(s =>
                    s.SurveyId == id &&
                    s.IsActive);

            if (survey == null)
                return NotFound();

            return View(survey);
        }


        // =====================================================
        // Збереження відповідей
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int id,
            int question1,
            int question2,
            int question3,
            int question4,
            int question5)
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s =>
                    s.SurveyId == id &&
                    s.IsActive);

            if (survey == null)
                return NotFound();


            // Перевірка відповідей

            var answers = new[]
            {
                question1,
                question2,
                question3,
                question4,
                question5
            };

            if (answers.Any(x => x < 1 || x > 5))
            {
                ModelState.AddModelError(
                    "",
                    "Усі питання повинні мати оцінку від 1 до 5.");

                return View("Index", survey);
            }


            // Створення відповідей

            for (int i = 0; i < answers.Length; i++)
            {
                var answer = new SurveyAnswer
                {
                    SurveyId = survey.SurveyId,

                    QuestionNumber = i + 1,

                    Score = answers[i],

                    CreatedAt = DateTime.Now
                };

                _context.SurveyAnswers.Add(answer);
            }

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Дякуємо! Ваші відповіді анонімно збережено.";

            return RedirectToAction(
                nameof(Result),
                new { id = survey.SurveyId });
        }

        [HttpGet]
        public async Task<IActionResult> Create(int teamId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound();

            var survey = new Survey
            {
                TeamId = teamId,
                Title = "Анонімне опитування психологічного клімату",
                Description =
                    "Оцініть комфорт, взаємодію, розподіл роботи, " +
                    "можливість висловлювати власну думку та загальну " +
                    "атмосферу в команді.",
                IsActive = true
            };

            _context.Surveys.Add(survey);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { id = survey.SurveyId });
        }


        // =====================================================
        // Результат
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Team)
                .FirstOrDefaultAsync(s => s.SurveyId == id);

            if (survey == null)
                return NotFound();

            var answers = await _context.SurveyAnswers
                .Where(a => a.SurveyId == id)
                .ToListAsync();

            ViewBag.Answers = answers;

            return View(survey);
        }
    }
}