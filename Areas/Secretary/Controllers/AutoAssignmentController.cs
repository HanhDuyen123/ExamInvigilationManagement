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
        private readonly ISemesterService _semesterService;
        private readonly IPeriodService _periodService;

        public AutoAssignmentController(
            IAutoAssignmentService autoAssignmentService,
            ISemesterService semesterService,
            IPeriodService periodService)
        {
            _autoAssignmentService = autoAssignmentService;
            _semesterService = semesterService;
            _periodService = periodService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? semesterId, int? periodId)
        {
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
                PeriodId = periodId
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
