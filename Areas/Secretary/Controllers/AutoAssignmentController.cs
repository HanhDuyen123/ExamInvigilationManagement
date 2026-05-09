using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamInvigilationManagement.Areas.Secretary.Controllers
{
    [Area("Secretary")]
    [Authorize(Roles = "Thư ký khoa")]
    public class AutoAssignmentController : Controller
    {
        private readonly IAutoAssignmentService _autoAssignmentService;

        public AutoAssignmentController(IAutoAssignmentService autoAssignmentService)
        {
            _autoAssignmentService = autoAssignmentService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new AutoAssignRequestDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(AutoAssignRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View("Index", request);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var assignerId))
            {
                ModelState.AddModelError(string.Empty, "Không xác định được người dùng hiện tại.");
                return View("Index", request);
            }

            request.AssignerId = assignerId;

            try
            {
                var result = await _autoAssignmentService.AutoAssignAsync(request, cancellationToken);
                return View("Result", result);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Index", request);
            }
        }
    }
}
