using ExamInvigilationManagement.Application.DTOs.ManualAssignment;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamInvigilationManagement.Areas.Secretary.Controllers
{
    [Area("Secretary")]
    [Authorize(Roles = "Thư ký khoa")]
    public class ManualAssignmentController : Controller
    {
        private readonly IManualAssignmentService _manualAssignmentService;
        private readonly IInvigilatorSubstitutionService _substitutionService;

        public ManualAssignmentController(IManualAssignmentService manualAssignmentService, IInvigilatorSubstitutionService substitutionService)
        {
            _manualAssignmentService = manualAssignmentService;
            _substitutionService = substitutionService;
        }

        [HttpGet]
        public async Task<IActionResult> Assign(int scheduleId, int? substitutionId, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var assignerId))
                return Unauthorized();

            try
            {
                if (scheduleId <= 0 && substitutionId.HasValue)
                {
                    var detail = await _substitutionService.GetDetailAsync(substitutionId.Value, assignerId, cancellationToken);
                    scheduleId = detail.ExamScheduleId;
                }

                var page = await _manualAssignmentService.GetPageAsync(scheduleId, assignerId, cancellationToken);
                if (substitutionId.HasValue)
                {
                    page.SubstitutionReview = await _substitutionService.GetDetailAsync(substitutionId.Value, assignerId, cancellationToken);
                    page.LecturerOptions = page.SubstitutionReview.ReplacementOptions;
                }
                return View(page);
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction("Index", "Dashboard", new { area = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSubstitution(int substitutionId, int replacementUserId, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var assignerId)) return Unauthorized();

            var result = await _substitutionService.ApproveWithReplacementAsync(substitutionId, replacementUserId, assignerId, cancellationToken);
            TempData.SetNotification(result.Success ? "success" : "error", result.Message);

            try
            {
                var detail = await _substitutionService.GetDetailAsync(substitutionId, assignerId, cancellationToken);
                return RedirectToAction(nameof(Assign), new { scheduleId = detail.ExamScheduleId, substitutionId });
            }
            catch
            {
                return RedirectToAction("Index", "InvigilatorSubstitution", new { area = "Secretary" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(ManualAssignmentRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var assignerId))
                return Unauthorized();

            request.AssignerId = assignerId;

            var result = await _manualAssignmentService.AssignAsync(request, cancellationToken);

            if (!result.Success)
            {
                TempData.SetNotification("error", result.Message);
                var page = await _manualAssignmentService.GetPageAsync(request.ExamScheduleId, assignerId, cancellationToken);
                page.Request = request;

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                return View(page);
            }

            TempData.SetNotification("success", result.Message);
            return RedirectToAction(nameof(Assign), new { scheduleId = request.ExamScheduleId });
        }
    }
}
