using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ExamInvigilationManagement.Common.Security
{
    public class RequireRecentAuthenticationFilter : IAuthorizationFilter
    {
        private static readonly TimeSpan RecentAuthenticationWindow = TimeSpan.FromMinutes(10);

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var recentAuthValue = user.FindFirst(AuthSessionClaimTypes.RecentAuthenticationUtc)?.Value;
            var isRecent = DateTimeOffset.TryParse(recentAuthValue, out var recentAuthUtc)
                && DateTimeOffset.UtcNow - recentAuthUtc <= RecentAuthenticationWindow;

            if (isRecent)
            {
                return;
            }

            var request = context.HttpContext.Request;
            var returnUrl = request.PathBase + request.Path + request.QueryString;

            context.Result = new RedirectToActionResult(
                "Reauthenticate",
                "Account",
                new { area = "", returnUrl });
        }
    }
}
