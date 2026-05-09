using Microsoft.AspNetCore.Mvc.Razor;
using System.Security.Claims;

namespace ExamInvigilationManagement.Infrastructure.UI
{
    public class RoleViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var user = context.ActionContext.HttpContext.User;

            context.Values["role"] =
                user.IsInRole("Lecturer") ? "Lecturer" :
                user.IsInRole("Secretary") ? "Secretary" :
                user.IsInRole("Dean") ? "Dean" :
                user.IsInRole("Admin") ? "Admin" :
                "Shared";
        }

        public IEnumerable<string> ExpandViewLocations(
            ViewLocationExpanderContext context,
            IEnumerable<string> viewLocations)
        {
            var role = context.Values.TryGetValue("role", out var r) ? r : "Shared";

            var customLocations = new[]
            {
                $"/Areas/{role}/Views/{{1}}/{{0}}.cshtml",
                $"/Areas/{role}/Views/Shared/{{0}}.cshtml",
                "/Views/Shared/{1}/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
                "/Views/{1}/{0}.cshtml"
            };

            return customLocations.Concat(viewLocations);
        }
    }
}