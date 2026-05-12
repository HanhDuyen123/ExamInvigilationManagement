using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.InvigilatorResponse;
using ExamInvigilationManagement.Application.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Giảng viên")]
    public class InvigilatorResponseController : Controller
    {
        private readonly IInvigilatorResponseService _service;

        public InvigilatorResponseController(IInvigilatorResponseService service)
        {
            _service = service;
        }

        public IActionResult Index(string? status = null)
        {
            ViewData["Title"] = "Xác nhận lịch coi thi";
            ViewBag.InitialStatus = status ?? string.Empty;
            return View(new InvigilatorAssignmentIndexDto());
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            InvigilatorAssignmentSearchDto search,
            int page = 1,
            int pageSize = 5,
            string viewMode = "table",
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _service.GetAssignmentsAsync(userId.Value, search, page, pageSize, cancellationToken);
            return PartialView(viewMode == "calendar" ? "_AssignmentCalendar" : "_AssignmentTable", result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromBody] InvigilatorResponseSubmitDto request, CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { success = false, message = "Không xác định được giảng viên hiện tại." });

            var result = await _service.SubmitAsync(userId.Value, request, cancellationToken);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, message = result.Message });
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
