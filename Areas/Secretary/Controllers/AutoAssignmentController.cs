using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
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
        private readonly ISemesterService _semesterService;
        private readonly IPeriodService _periodService;
        private readonly ICurrentAcademicContextService _currentAcademicContextService;

        public AutoAssignmentController(
            IAutoAssignmentService autoAssignmentService,
            ISemesterService semesterService,
            IPeriodService periodService,
            ICurrentAcademicContextService currentAcademicContextService)
        {
            _autoAssignmentService = autoAssignmentService;
            _semesterService = semesterService;
            _periodService = periodService;
            _currentAcademicContextService = currentAcademicContextService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? semesterId, int? periodId)
        {
            if (!semesterId.HasValue && !periodId.HasValue)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var context = await _currentAcademicContextService.GetCurrentContextAsync(userId, "Thư ký khoa");
                    semesterId = context?.SemesterId;
                    periodId = context?.PeriodId;
                }
            }

            if (semesterId.HasValue && semesterId.Value > 0)
            {
                var semester = (await _semesterService.GetAllAsync()).FirstOrDefault(x => x.Id == semesterId.Value);
                if (semester != null)
                {
                    ViewBag.InitialAcademyYearId = semester.AcademyYearId;
                    ViewBag.InitialAcademyYearName = semester.AcademicYear;
                    ViewBag.InitialSemesterName = semester.Name;
                }
            }

            if (periodId.HasValue && periodId.Value > 0 && semesterId.HasValue && semesterId.Value > 0)
            {
                var period = (await _periodService.GetAllBySemesterAsync(semesterId.Value)).FirstOrDefault(x => x.Id == periodId.Value);
                ViewBag.InitialPeriodName = period?.Name;
            }

            return View(new AutoAssignRequestDto
            {
                SemesterId = semesterId,
                PeriodId = periodId,
                RunSeed = Random.Shared.Next(1, int.MaxValue)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(AutoAssignRequestDto request, CancellationToken cancellationToken)
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
                var result = await _autoAssignmentService.PreviewAsync(request, cancellationToken);
                return View("Result", result);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Index", request);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(AutoAssignRequestDto request, CancellationToken cancellationToken)
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
                var result = await _autoAssignmentService.SaveDraftAsync(request, cancellationToken);
                TempData.SetNotification("success", "Đã lưu bản tạm để so sánh với lần chạy sau.");
                return View("Result", result);
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction(nameof(Index), new { semesterId = request.SemesterId, periodId = request.PeriodId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearDraft(AutoAssignRequestDto request, CancellationToken cancellationToken)
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
                var result = await _autoAssignmentService.ClearDraftAsync(request, cancellationToken);
                TempData.SetNotification("success", "Đã xoá bản tạm.");
                return View("Result", result);
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction(nameof(Index), new { semesterId = request.SemesterId, periodId = request.PeriodId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompareDraft(AutoAssignRequestDto request, CancellationToken cancellationToken)
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
                var result = await _autoAssignmentService.CompareDraftAsync(request, cancellationToken);
                return View("Result", result);
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction(nameof(Index), new { semesterId = request.SemesterId, periodId = request.PeriodId });
            }
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
