using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.InvigilatorSubstitution;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Giảng viên")]
    public class InvigilatorSubstitutionController : Controller
    {
        private readonly IInvigilatorSubstitutionService _service;

        public InvigilatorSubstitutionController(IInvigilatorSubstitutionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int examInvigilatorId, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            try
            {
                var page = await _service.GetCreatePageAsync(examInvigilatorId, userId.Value, cancellationToken);
                return View(page);
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction("Index", "InvigilatorResponse", new { area = "Lecturer" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvigilatorSubstitutionCreateRequestDto request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            InvigilatorSubstitutionResultDto result;
            try
            {
                result = await _service.CreateAsync(request, userId.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction("Index", "InvigilatorResponse", new { area = "Lecturer", status = "Từ chối" });
            }

            if (!result.Success)
            {
                TempData.SetNotification("error", result.Message);
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error);
                try
                {
                    var page = await _service.GetCreatePageAsync(request.ExamInvigilatorId, userId.Value, cancellationToken);
                    page.Request = request;
                    return View(page);
                }
                catch
                {
                    return RedirectToAction("Index", "InvigilatorResponse", new { area = "Lecturer", status = "Từ chối" });
                }
            }

            TempData.SetNotification("success", result.Message);
            return RedirectToAction("Index", "InvigilatorResponse", new { area = "Lecturer", status = "Từ chối" });
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
