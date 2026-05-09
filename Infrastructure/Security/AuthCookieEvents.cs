using ExamInvigilationManagement.Common.Security;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExamInvigilationManagement.Infrastructure.Security
{
    public class AuthCookieEvents : CookieAuthenticationEvents
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthCookieEvents> _logger;

        public AuthCookieEvents(ApplicationDbContext context, ILogger<AuthCookieEvents> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                await RejectAsync(context, "missing-user-id");
                return;
            }

            var user = await _context.Users
                .Include(x => x.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null || !user.IsActive)
            {
                await RejectAsync(context, "inactive-or-missing-user");
                return;
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.Now)
            {
                await RejectAsync(context, "locked-user");
                return;
            }

            var issuedRole = context.Principal?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var currentRole = user.Role?.RoleName ?? string.Empty;
            if (!string.Equals(issuedRole, currentRole, StringComparison.Ordinal))
            {
                await RejectAsync(context, "role-changed");
                return;
            }

            var issuedPasswordVersion = context.Principal?.FindFirstValue(AuthSessionClaimTypes.PasswordVersion) ?? string.Empty;
            var currentPasswordVersion = AuthSessionVersion.FromPasswordHash(user.UserId, user.PasswordHash);
            if (!string.Equals(issuedPasswordVersion, currentPasswordVersion, StringComparison.Ordinal))
            {
                await RejectAsync(context, "password-changed");
                return;
            }
        }

        private async Task RejectAsync(CookieValidatePrincipalContext context, string reason)
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

            _logger.LogWarning("Authentication cookie rejected. UserId={UserId}, Reason={Reason}", userId, reason);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
