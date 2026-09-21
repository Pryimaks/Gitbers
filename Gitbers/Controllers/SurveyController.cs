using System.Security.Claims;
using Gitbers.Data;
using Gitbers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
    [Authorize]
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

            // =================================================
            // Перевірка відповідей
            // =================================================

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
                var surveyForView = await _context.Surveys
                    .Include(s => s.Team)
                    .FirstOrDefaultAsync(s =>
                        s.SurveyId == id &&
                        s.IsActive);

                if (surveyForView == null)
                    return NotFound();

                ModelState.AddModelError(
                    "",
                    "Усі питання повинні мати оцінку від 1 до 5.");

                return View("Index", surveyForView);
            }

            // =================================================
            // Створення анонімних відповідей
            // =================================================

            for (int i = 0; i < answers.Length; i++)
            {
                var answer = new SurveyAnswer
                {
                    SurveyId = survey.SurveyId,
                    QuestionNumber = i + 1,
                    Score = answers[i],
                    CreatedAt = DateTime.Now
                };

                // UserId тут НЕ зберігається.
                // Відповідь залишається анонімною.

                _context.SurveyAnswers.Add(answer);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Дякуємо! Ваші відповіді анонімно збережено.";

            // Не показуємо Result респонденту.
            // Result доступний тільки власнику команди.
            return RedirectToAction(
                nameof(Index),
                new { id = survey.SurveyId });
        }

        // =====================================================
        // Створення опитування
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create(int teamId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Створювати опитування може тільки власник команди
            var team = await _context.Teams
                .FirstOrDefaultAsync(t =>
                    t.TeamId == teamId &&
                    t.OwnerId == userId.Value);

            if (team == null)
                return NotFound();

            var survey = new Survey
            {
                TeamId = teamId,

                Title =
                    "Анонімне опитування психологічного клімату",

                Description =
                    "Оцініть комфорт, взаємодію, розподіл роботи, " +
                    "можливість висловлювати власну думку та загальну " +
                    "атмосферу в команді.",

                IsActive = true,

                CreatedAt = DateTime.Now
            };

            _context.Surveys.Add(survey);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { id = survey.SurveyId });
        }

        // =====================================================
        // Результат опитування
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Отримуємо опитування тільки власної команди
            var survey = await _context.Surveys
                .Include(s => s.Team)
                .FirstOrDefaultAsync(s =>
                    s.SurveyId == id &&
                    s.Team != null &&
                    s.Team.OwnerId == userId.Value);

            if (survey == null)
                return NotFound();

            var answers = await _context.SurveyAnswers
                .Where(a => a.SurveyId == id)
                .OrderBy(a => a.QuestionNumber)
                .ToListAsync();

            ViewBag.Answers = answers;

            return View(survey);
        }

        // =====================================================
        // Поточний користувач
        // =====================================================

        private int? GetCurrentUserId()
        {
            var userIdString =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (int.TryParse(
                userIdString,
                out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}