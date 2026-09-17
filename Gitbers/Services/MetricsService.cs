using Gitbers.Data;
using Gitbers.Models;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Services
{
    public class MetricsService
    {
        private readonly ApplicationDbContext _context;

        public MetricsService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // Розрахунок метрик команди
        // =====================================================

        public async Task<MetricSnapshot?> CalculateTeamMetricsAsync(
            int teamId,
            DateTime periodStart,
            DateTime periodEnd)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return null;

            if (!team.Members.Any())
                return null;

            // Отримуємо активність учасників за вибраний період.
            var activities = await _context.GitHubActivities
                .Where(a =>
                    team.Members
                        .Select(m => m.TeamMemberId)
                        .Contains(a.TeamMemberId)
                    &&
                    a.ActivityDate >= periodStart
                    &&
                    a.ActivityDate <= periodEnd)
                .ToListAsync();

            if (!activities.Any())
                return null;

            // =================================================
            // 1. ACTIVITY SCORE
            // =================================================

            double activityScore =
                CalculateActivityScore(activities);


            // =================================================
            // 2. COLLABORATION SCORE
            // =================================================

            double collaborationScore =
                CalculateCollaborationScore(activities);


            // =================================================
            // 3. STABILITY SCORE
            // =================================================

            double stabilityScore =
                CalculateStabilityScore(activities);


            // =================================================
            // 4. RISK SCORE
            // =================================================

            double riskScore =
                CalculateRiskScore(
                    activityScore,
                    collaborationScore,
                    stabilityScore);


            // =================================================
            // Рівень ризику
            // =================================================

            string riskLevel =
                GetRiskLevel(riskScore);


            // =================================================
            // Створення snapshot
            // =================================================

            var snapshot = new MetricSnapshot
            {
                TeamId = teamId,

                PeriodStart = periodStart,
                PeriodEnd = periodEnd,

                ActivityScore = Math.Round(
                    activityScore, 2),

                CollaborationScore = Math.Round(
                    collaborationScore, 2),

                StabilityScore = Math.Round(
                    stabilityScore, 2),

                RiskScore = Math.Round(
                    riskScore, 2),

                RiskLevel = riskLevel,

                CreatedAt = DateTime.Now
            };

            _context.MetricSnapshots.Add(snapshot);

            await _context.SaveChangesAsync();

            return snapshot;
        }


        // =====================================================
        // Activity Score
        // =====================================================

        private double CalculateActivityScore(
            List<GitHubActivity> activities)
        {
            if (!activities.Any())
                return 0;

            double totalCommits =
                activities.Sum(a => a.CommitsCount);

            double totalPullRequests =
                activities.Sum(a => a.PullRequestsCount);

            /*
             * Для MVP використовуємо нормалізацію
             * відносно заданого базового рівня.
             *
             * 10 commits = 100%
             * 3 pull requests = 100%
             */

            double commitsScore =
                Normalize(
                    totalCommits,
                    10);

            double pullRequestsScore =
                Normalize(
                    totalPullRequests,
                    3);

            return
                commitsScore * 0.60 +
                pullRequestsScore * 0.40;
        }


        // =====================================================
        // Collaboration Score
        // =====================================================

        private double CalculateCollaborationScore(
            List<GitHubActivity> activities)
        {
            if (!activities.Any())
                return 0;

            double totalPullRequests =
                activities.Sum(a => a.PullRequestsCount);

            double totalReviews =
                activities.Sum(a => a.ReviewsCount);

            /*
             * Code Review є основним показником
             * взаємодії між розробниками.
             */

            double pullRequestsScore =
                Normalize(
                    totalPullRequests,
                    3);

            double reviewsScore =
                Normalize(
                    totalReviews,
                    5);

            return
                pullRequestsScore * 0.40 +
                reviewsScore * 0.60;
        }


        // =====================================================
        // Stability Score
        // =====================================================

        private double CalculateStabilityScore(
            List<GitHubActivity> activities)
        {
            if (activities.Count <= 1)
                return 100;

            var memberActivity = activities
                .GroupBy(a => a.TeamMemberId)
                .Select(g =>
                    g.Sum(a =>
                        a.CommitsCount +
                        a.PullRequestsCount +
                        a.IssuesCount +
                        a.ReviewsCount))
                .ToList();

            if (!memberActivity.Any())
                return 0;

            double average =
                memberActivity.Average();

            if (average == 0)
                return 0;

            double variance =
                memberActivity
                    .Select(x =>
                        Math.Pow(x - average, 2))
                    .Average();

            double standardDeviation =
                Math.Sqrt(variance);

            double coefficientOfVariation =
                standardDeviation / average;

            double stability =
                100 * (1 - coefficientOfVariation);

            return Math.Clamp(
                stability,
                0,
                100);
        }


        // =====================================================
        // Risk Score
        // =====================================================

        private double CalculateRiskScore(
            double activityScore,
            double collaborationScore,
            double stabilityScore)
        {
            double risk =
                (100 - activityScore) * 0.40 +
                (100 - collaborationScore) * 0.40 +
                (100 - stabilityScore) * 0.20;

            return Math.Clamp(
                risk,
                0,
                100);
        }


        // =====================================================
        // Risk Level
        // =====================================================

        private string GetRiskLevel(
            double riskScore)
        {
            if (riskScore < 30)
                return "Low";

            if (riskScore < 60)
                return "Medium";

            return "High";
        }


        // =====================================================
        // Нормалізація показника
        // =====================================================

        private double Normalize(
            double value,
            double maximum)
        {
            if (maximum <= 0)
                return 0;

            return Math.Clamp(
                value / maximum * 100,
                0,
                100);
        }
    }
}