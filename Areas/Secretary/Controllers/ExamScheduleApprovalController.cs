using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.Approval;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Secretary.Controllers
{
    [Area("Secretary")]
    [Authorize(Roles = "Trưởng khoa,Thư ký khoa")]
    public class ExamScheduleApprovalController : Controller
    {
        private readonly IExamScheduleApprovalService _approvalService;
        private readonly IAdminUserService _userService;

        public ExamScheduleApprovalController(
            IExamScheduleApprovalService approvalService,
            IAdminUserService userService)
        {
            _approvalService = approvalService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] ExamScheduleApprovalSearchDto search,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            var pageModel = await _approvalService.GetIndexAsync(search, userId.Value, page, pageSize, cancellationToken);
            return View(pageModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] ExamScheduleApprovalSearchDto search,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            var pageModel = await _approvalService.GetIndexAsync(search, userId.Value, page, pageSize, cancellationToken);
            return PartialView("_ExamScheduleApprovalTable", pageModel.PagedItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(ExamScheduleApprovalIndexPageDto model, CancellationToken cancellationToken)
        {
            return await HandleBulkReviewAsync(model, true, cancellationToken);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkReject(ExamScheduleApprovalIndexPageDto model, CancellationToken cancellationToken)
        {
            return await HandleBulkReviewAsync(model, false, cancellationToken);
        }

        private async Task<IActionResult> HandleBulkReviewAsync(
            ExamScheduleApprovalIndexPageDto model,
            bool isApproved,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            var request = new ExamScheduleApprovalBulkReviewRequestDto
            {
                SelectedExamScheduleIds = model.SelectedExamScheduleIds ?? new List<int>(),
                IsApproved = isApproved,
                Note = model.BulkNote
            };

            var result = await _approvalService.ReviewBulkAsync(request, userId.Value, cancellationToken);

            if (!result.Success)
            {
                var page = await _approvalService.GetIndexAsync(
                    model.Search ?? new ExamScheduleApprovalSearchDto(),
                    userId.Value,
                    model.Page,
                    model.PageSize,
                    cancellationToken);

                page.SelectedExamScheduleIds = model.SelectedExamScheduleIds ?? new List<int>();
                page.BulkNote = model.BulkNote;

                var toastMessage = result.Errors.FirstOrDefault() ?? result.Message;
                TempData.SetNotification("error", toastMessage);

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                ModelState.AddModelError(string.Empty, result.Message);
                return View("Index", page);
            }

            TempData.SetNotification("success", result.Message);
            return RedirectToAction(nameof(Index), model.Search);
        }

        [HttpGet]
        public async Task<IActionResult> SearchLecturer(string? keyword, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            var currentUser = await _userService.GetByIdAsync(userId.Value);
            if (currentUser == null)
                return Unauthorized();

            var paged = await _userService.GetPagedAsync(null, null, null, null, null, 1, 1000);
            var lecturers = paged.Items
                .Where(x => string.Equals(x.RoleName, "Giảng viên", StringComparison.OrdinalIgnoreCase));

            if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                lecturers = lecturers.Where(x => x.FacultyId == currentUser.FacultyId);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                lecturers = lecturers.Where(x =>
                    (x.FullName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            return Json(lecturers.Select(x => new
            {
                id = x.Id,
                name = x.FullName ?? x.UserName
            }));
        }

        private int? GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var userId) ? userId : null;
        }
    }
}
