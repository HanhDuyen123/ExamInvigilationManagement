using ExamInvigilationManagement.Application.DTOs.Admin.User;
using ExamInvigilationManagement.Application.DTOs.Admin.Faculty;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize(Roles = "Admin,Trưởng khoa,Thư ký khoa")]
    public class LecturerManagementController : BaseRoleController
    {
        private readonly IFacultyService _facultyService;
        private readonly IPositionService _positionService;
        private readonly ILecturerBusySlotService _busySlotService;

        public LecturerManagementController(
            IAdminUserService userService,
            IFacultyService facultyService,
            IPositionService positionService,
            ILecturerBusySlotService busySlotService) : base(userService)
        {
            _facultyService = facultyService;
            _positionService = positionService;
            _busySlotService = busySlotService;
        }

        [Authorize(Roles = "Admin,Trưởng khoa")]
        public IActionResult Index()
        {
            ViewBag.ShowFacultyFilter = User.IsInRole("Admin");
            ViewBag.AvailabilityUrl = Url.Action(nameof(Availability), "LecturerManagement");
            var model = new CrudIndexViewModel
            {
                Title = "Quản lý giảng viên",
                Subtitle = "Quản lý mã giảng viên, hồ sơ, khoa/viện và trạng thái tham gia coi thi.",
                CreateUrl = Url.Action(nameof(Create), "LecturerManagement"),
                SearchPartialView = "_LecturerManagementSearch",
                TableClass = "full-width"
            };

            return View(model);
        }

        [Authorize(Roles = "Admin,Trưởng khoa")]
        public async Task<IActionResult> GetList(string? keyword, int? facultyId, string? status, int page = 1, int pageSize = 5)
        {
            bool? isActive = status switch
            {
                "true" => true,
                "false" => false,
                _ => null
            };

            var scopeFacultyId = await GetFacultyScopeAsync();
            var result = await _userService.GetLecturersPagedAsync(keyword, scopeFacultyId ?? facultyId, isActive, page, pageSize);
            return PartialView("_LecturerManagementTable", result);
        }

        [HttpGet]
        public IActionResult Availability()
        {
            ViewBag.ShowFacultyFilter = User.IsInRole("Admin");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailabilityList(string? keyword, int? facultyId, int? academyYearId, int? semesterId, int? periodId, int page = 1, int pageSize = 10)
        {
            var scopeFacultyId = await GetFacultyScopeAsync();
            var result = await _busySlotService.GetAvailabilityPagedAsync(new Application.DTOs.LecturerBusySlot.LecturerPeriodAvailabilitySearchDto
            {
                Keyword = keyword,
                FacultyId = scopeFacultyId ?? facultyId,
                AcademyYearId = academyYearId,
                SemesterId = semesterId,
                PeriodId = periodId
            }, page, pageSize);

            return PartialView("_LecturerAvailabilityTable", result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Trưởng khoa")]
        public IActionResult Create()
        {
            var dto = new LecturerManagementDto
            {
                FacultyId = GetCurrentFacultyId(),
                IsActive = true
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Trưởng khoa")]
        public async Task<IActionResult> Create(LecturerManagementDto dto)
        {
            if (User.IsInRole("Trưởng khoa"))
                dto.FacultyId = await GetFacultyScopeAsync();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _userService.CreateLecturerAsync(dto, await GetFacultyScopeAsync());
                TempData.SetNotification("success", "Thêm giảng viên thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Trưởng khoa")]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _userService.GetLecturerByIdAsync(id, await GetFacultyScopeAsync());
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy giảng viên cần cập nhật.");
                return RedirectToAction(nameof(Index));
            }

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Trưởng khoa")]
        public async Task<IActionResult> Edit(LecturerManagementDto dto)
        {
            if (User.IsInRole("Trưởng khoa"))
                dto.FacultyId = await GetFacultyScopeAsync();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _userService.UpdateLecturerAsync(dto, await GetFacultyScopeAsync());
                TempData.SetNotification("success", "Cập nhật giảng viên thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Trưởng khoa")]
        public async Task<IActionResult> Delete(int id)
        {
            var lecturer = await _userService.GetLecturerByIdAsync(id, await GetFacultyScopeAsync());
            if (lecturer == null)
            {
                TempData.SetNotification("error", "Không tìm thấy giảng viên cần vô hiệu hóa.");
                return RedirectToAction(nameof(Index));
            }

            await _userService.SetActiveAsync(id, false);
            TempData.SetNotification("success", "Đã vô hiệu hóa giảng viên.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SearchFaculties(string? keyword)
        {
            var data = await _facultyService.GetAllAsync();
            if (!User.IsInRole("Admin"))
            {
                var currentFacultyId = await GetCurrentFacultyIdAsync();
                data = currentFacultyId.HasValue
                    ? data.Where(x => x.Id == currentFacultyId.Value).ToList()
                    : new List<FacultyDto>();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data.Where(x => x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Json(data.Select(x => new { id = x.Id, name = x.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> SearchPositions(string? keyword)
        {
            var data = await _positionService.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data.Where(x => x.PositionName.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Json(data.Select(x => new { id = x.PositionId, name = x.PositionName }));
        }

        private async Task<int?> GetFacultyScopeAsync()
        {
            if (!User.IsInRole("Trưởng khoa")) return null;
            return await GetCurrentFacultyIdAsync();
        }
    }
}
