using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.InvigilatorResponse;
using ExamInvigilationManagement.Application.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Giảng viên,Trưởng khoa,Thư ký khoa")]
    public class InvigilatorResponseController : Controller
    {
        private readonly IInvigilatorResponseService _service;
        private readonly ICurrentAcademicContextService _currentAcademicContextService;

        public InvigilatorResponseController(IInvigilatorResponseService service, ICurrentAcademicContextService currentAcademicContextService)
        {
            _service = service;
            _currentAcademicContextService = currentAcademicContextService;
        }

        public async Task<IActionResult> Index(string? status = null, CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            var context = userId.HasValue
                ? await _currentAcademicContextService.GetCurrentContextAsync(userId.Value, "Giảng viên", null, cancellationToken)
                : null;

            ViewData["Title"] = "Xác nhận lịch coi thi";
            ViewBag.InitialStatus = status ?? string.Empty;
            ViewBag.InitialAcademyYearName = context?.AcademyYearName;
            ViewBag.InitialSemesterName = context?.SemesterName;
            ViewBag.InitialPeriodName = context?.PeriodName;
            return View(new InvigilatorAssignmentIndexDto
            {
                Search = new InvigilatorAssignmentSearchDto
                {
                    AcademyYearId = context?.AcademyYearId,
                    SemesterId = context?.SemesterId,
                    PeriodId = context?.PeriodId,
                    Status = status
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            InvigilatorAssignmentSearchDto search,
            int page = 1,
            int pageSize = 5,
            string viewMode = "table",
            DateTime? weekStart = null,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            if (string.Equals(viewMode, "calendar", StringComparison.OrdinalIgnoreCase))
            {
                var calendar = await _service.GetAssignmentCalendarWeekAsync(userId.Value, search, weekStart, cancellationToken);
                return PartialView("_AssignmentCalendar", calendar);
            }

            var result = await _service.GetAssignmentsAsync(userId.Value, search, page, pageSize, cancellationToken);
            return PartialView("_AssignmentTable", result);
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

        private async Task ApplyDefaultAcademicContextAsync(int userId, InvigilatorAssignmentSearchDto search, CancellationToken cancellationToken)
        {
            if (search.AcademyYearId.HasValue || search.SemesterId.HasValue || search.PeriodId.HasValue || search.FromDate.HasValue || search.ToDate.HasValue)
                return;

            var context = await _currentAcademicContextService.GetCurrentContextAsync(userId, "Giảng viên", null, cancellationToken);
            if (context is null)
                return;

            search.AcademyYearId = context.AcademyYearId;
            search.SemesterId = context.SemesterId;
            search.PeriodId = context.PeriodId;
        }
    }
}
