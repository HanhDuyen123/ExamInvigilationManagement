using System.Security.Claims;
using ExamInvigilationManagement.Application.Interfaces.Service;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    public abstract class BaseRoleController : Controller
    {
        protected readonly IAdminUserService _userService;

        protected BaseRoleController(IAdminUserService userService)
        {
            _userService = userService;
        }

        protected int? GetCurrentUserId()
        {
            var raw =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("UserId");

            return int.TryParse(raw, out var id) ? id : null;
        }

        protected int? GetCurrentFacultyId()
        {
            var raw = User.FindFirstValue("FacultyId");
            return int.TryParse(raw, out var id) ? id : null;
        }

        protected async Task<int?> GetCurrentFacultyIdAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return null;

            var user = await _userService.GetByIdAsync(userId.Value);
            return user?.FacultyId;
        }

        protected bool IsAdmin()
            => User.IsInRole("Admin");

        protected bool IsLecturer()
            => User.IsInRole("Giảng viên");

        protected bool IsFacultyManager()
            => User.IsInRole("Thư ký khoa") ||
               User.IsInRole("Trưởng khoa");
    }
}