using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Secretary.Controllers
{
    [Area("Secretary")]
    [Authorize(Roles = "Thư ký khoa")]
    public class AutoAssignmentPolicyController : Controller
    {
        private readonly IAutoAssignmentPolicyService _policyService;

        public AutoAssignmentPolicyController(IAutoAssignmentPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var actorUserId = GetCurrentUserId();
            if (!actorUserId.HasValue)
                return Unauthorized();

            var model = await _policyService.GetDefaultPolicyAsync(actorUserId.Value, cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(AutoAssignmentPolicyEditDto model, CancellationToken cancellationToken)
        {
            var actorUserId = GetCurrentUserId();
            if (!actorUserId.HasValue)
                return Unauthorized();

            if (!ModelState.IsValid)
                return View("Index", model);

            try
            {
                await _policyService.UpdateDefaultPolicyAsync(model, actorUserId.Value, cancellationToken);
                TempData.SetNotification("success", "Đã cập nhật chính sách phân công tự động của khoa.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                TempData.SetNotification("error", ex.Message);
                return View("Index", model);
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
