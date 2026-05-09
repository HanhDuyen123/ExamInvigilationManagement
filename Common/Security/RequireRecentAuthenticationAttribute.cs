using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Common.Security
{
    public class RequireRecentAuthenticationAttribute : TypeFilterAttribute
    {
        public RequireRecentAuthenticationAttribute() : base(typeof(RequireRecentAuthenticationFilter))
        {
        }
    }
}
