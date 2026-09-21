using Gitbers.Data;
using Gitbers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gitbers.Controllers
{
    [ApiController]
    [Route("Viber")]
    public class ViberController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ViberController> _logger;

        public ViberController(
            ApplicationDbContext context,
            ILogger<ViberController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("Webhook")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook(
            [FromBody] ViberWebhookRequest request)
        {
            _logger.LogInformation(
                "Viber webhook event: {Event}",
                request.Event);

            if (request.User == null ||
                string.IsNullOrWhiteSpace(request.User.Id))
            {
                return Ok();
            }

            var viberUserId = request.User.Id;

            // =====================================================
            // Користувач уже підключений
            // =====================================================

            var existingMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm =>
                    tm.ViberUserId == viberUserId);

            if (existingMember != null)
            {
                existingMember.ViberConnected = true;

                await _context.SaveChangesAsync();

                return Ok();
            }

            // =====================================================
            // Новий користувач — шукаємо код підключення
            // =====================================================

            if (request.Event == "message")
            {
                var connectionCode =
                    request.Message?.Text?.Trim();

                if (!string.IsNullOrWhiteSpace(connectionCode))
                {
                    var member = await _context.TeamMembers
                        .FirstOrDefaultAsync(tm =>
                            tm.ViberConnectionCode ==
                                connectionCode &&
                            tm.ViberConnectionCodeExpiresAt != null &&
                            tm.ViberConnectionCodeExpiresAt >
                                DateTime.UtcNow);

                    if (member != null)
                    {
                        member.ViberUserId =
                            viberUserId;

                        member.ViberConnected = true;

                        member.ViberConnectionCode = null;

                        member.ViberConnectionCodeExpiresAt =
                            null;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation(
                            "Viber connected. TeamMemberId: {TeamMemberId}, ViberUserId: {ViberUserId}",
                            member.TeamMemberId,
                            viberUserId);
                    }
                }
            }

            return Ok();
        }
    }
}