using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize] // Bắt buộc login mới vào dashboard
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Lấy role name hiện tại
            var roleName = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            ViewBag.RoleName = roleName;

            return View();
        }
    }
}