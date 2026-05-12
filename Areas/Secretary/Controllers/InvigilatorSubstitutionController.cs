using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.InvigilatorSubstitution;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Secretary.Controllers
{
    [Area("Secretary")]
    [Authorize(Roles = "Thư ký khoa")]
    public class InvigilatorSubstitutionController : Controller
    {
        private readonly IInvigilatorSubstitutionService _service;

        public InvigilatorSubstitutionController(IInvigilatorSubstitutionService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(InvigilatorSubstitutionSearchDto search, int page = 1, int pageSize = 5, CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();
            var model = await _service.GetIndexAsync(userId.Value, search, page, pageSize, cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            try
            {
                var detail = await _service.GetDetailAsync(id, userId.Value, cancellationToken);
                return View(detail);
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();
            var result = await _service.ApproveAsync(id, userId.Value, cancellationToken);
            TempData.SetNotification(result.Success ? "success" : "error", result.Message);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();
            var result = await _service.RejectAsync(id, userId.Value, cancellationToken);
            TempData.SetNotification(result.Success ? "success" : "error", result.Message);
            return RedirectToAction(nameof(Details), new { id });
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
