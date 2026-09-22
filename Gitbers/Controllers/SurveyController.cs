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

            // Отримуємо всі анонімні відповіді
            var answers = await _context.SurveyAnswers
                .Where(a => a.SurveyId == id)
                .OrderBy(a => a.QuestionNumber)
                .ToListAsync();

            // Кількість повних проходжень опитування.
            // Кожен респондент залишає 5 відповідей.
            var respondentCount = answers.Count / 5;

            // Середні значення за кожним питанням
            var questionAverages = new double[5];

            for (int question = 1; question <= 5; question++)
            {
                var questionAnswers = answers
                    .Where(a => a.QuestionNumber == question)
                    .Select(a => a.Score)
                    .ToList();

                questionAverages[question - 1] =
                    questionAnswers.Any()
                        ? Math.Round(questionAnswers.Average(), 2)
                        : 0;
            }

            // Загальний показник психологічного клімату
            var climateScore = questionAverages
                .Where(x => x > 0)
                .DefaultIfEmpty(0)
                .Average();

            climateScore = Math.Round(climateScore, 2);

            // Інтерпретація показника
            string climateLevel;

            if (climateScore >= 4.0)
            {
                climateLevel = "Сприятливий";
            }
            else if (climateScore >= 3.0)
            {
                climateLevel = "Середній";
            }
            else
            {
                climateLevel = "Потребує уваги";
            }

            ViewBag.RespondentCount = respondentCount;
            ViewBag.QuestionAverages = questionAverages;
            ViewBag.ClimateScore = climateScore;
            ViewBag.ClimateLevel = climateLevel;

            return View(survey);
        }

        // =====================================================
        // Проходження опитування за персональним посиланням
        // =====================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Take(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return NotFound();

            var invitation = await _context.SurveyInvitations
                .Include(i => i.Survey)
                    .ThenInclude(s => s!.Team)
                .Include(i => i.TeamMember)
                    .ThenInclude(tm => tm!.User)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
                return NotFound();

            // Перевіряємо, чи опитування існує та активне
            if (invitation.Survey == null ||
                !invitation.Survey.IsActive)
            {
                return NotFound();
            }

            // Перевіряємо термін дії посилання
            if (invitation.ExpiresAt.HasValue &&
                invitation.ExpiresAt.Value < DateTime.UtcNow)
            {
                return View("InvitationExpired");
            }

            // Перевіряємо, чи опитування вже пройдено
            if (invitation.UsedAt.HasValue)
            {
                return View("InvitationUsed");
            }

            return View(invitation);
        }

        // =====================================================
        // Збереження відповідей за персональним посиланням
        // =====================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(
            string token,
            int question1,
            int question2,
            int question3,
            int question4,
            int question5)
        {
            if (string.IsNullOrWhiteSpace(token))
                return NotFound();

            var invitation = await _context.SurveyInvitations
                .Include(i => i.Survey)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
                return NotFound();

            if (invitation.Survey == null ||
                !invitation.Survey.IsActive)
            {
                return NotFound();
            }

            if (invitation.ExpiresAt.HasValue &&
                invitation.ExpiresAt.Value < DateTime.UtcNow)
            {
                return View("InvitationExpired");
            }

            if (invitation.UsedAt.HasValue)
            {
                return View("InvitationUsed");
            }

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

                return RedirectToAction(
                    nameof(Take),
                    new { token });
            }

            // Зберігаємо відповіді без TeamMemberId/UserId.
            // Тобто SurveyAnswer залишається анонімним.

            for (int i = 0; i < answers.Length; i++)
            {
                var answer = new SurveyAnswer
                {
                    SurveyId = invitation.SurveyId,
                    QuestionNumber = i + 1,
                    Score = answers[i],
                    CreatedAt = DateTime.Now
                };

                _context.SurveyAnswers.Add(answer);
            }

            // Позначаємо персональне посилання як використане
            invitation.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return View("Completed");
        }

        // =====================================================
        // Створення персонального посилання
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> CreateInvitation(
            int surveyId,
            int teamMemberId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var invitationData = await _context.SurveyInvitations
                .FirstOrDefaultAsync(i =>
                    i.SurveyId == surveyId &&
                    i.TeamMemberId == teamMemberId &&
                    !i.UsedAt.HasValue);

            if (invitationData != null)
            {
                var existingUrl = Url.Action(
                    nameof(Take),
                    "Survey",
                    new { token = invitationData.Token },
                    Request.Scheme);

                return Json(new
                {
                    success = true,
                    url = existingUrl
                });
            }

            var survey = await _context.Surveys
                .Include(s => s.Team)
                .FirstOrDefaultAsync(s =>
                    s.SurveyId == surveyId &&
                    s.IsActive &&
                    s.Team != null &&
                    s.Team.OwnerId == userId.Value);

            if (survey == null)
                return NotFound();

            var teamMember = await _context.TeamMembers
                .Include(tm => tm.Team)
                .FirstOrDefaultAsync(tm =>
                    tm.TeamMemberId == teamMemberId &&
                    tm.TeamId == survey.TeamId);

            if (teamMember == null)
                return NotFound();

            using var randomNumberGenerator =
                System.Security.Cryptography.RandomNumberGenerator.Create();

            var bytes = new byte[32];

            randomNumberGenerator.GetBytes(bytes);

            var token =
                Convert.ToHexString(bytes).ToLowerInvariant();

            var invitation = new SurveyInvitation
            {
                SurveyId = survey.SurveyId,
                TeamMemberId = teamMember.TeamMemberId,
                Token = token,
                CreatedAt = DateTime.UtcNow,

                // Посилання діє 7 днів
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.SurveyInvitations.Add(invitation);

            await _context.SaveChangesAsync();

            var url = Url.Action(
                nameof(Take),
                "Survey",
                new { token = invitation.Token },
                Request.Scheme);

            return Json(new
            {
                success = true,
                url
            });
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