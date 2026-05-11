using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize(Roles = "Admin,Trưởng khoa,Thư ký khoa,Giảng viên")]
    public class BusySlotController : BaseRoleController
    {
        private readonly ILecturerBusySlotService _service;

        public BusySlotController(
            ILecturerBusySlotService service,
            IAdminUserService userService
        ) : base(userService)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            ViewBag.ShowUserFilter = !User.IsInRole("Giảng viên");
            ViewBag.ShowActionColumn = User.IsInRole("Giảng viên");

            var vm = new CrudIndexViewModel
            {
                Title = "Lecturer Busy Slot Management",
                Subtitle = "Quản lý lịch bận giảng viên",
                CreateUrl = @Url.Action("Create", "BusySlot") ?? "#",
                SearchPartialView = "_BusySlotSearch",
                TableClass = "full-width",
                ShowCreateButton = User.IsInRole("Giảng viên"),
                ImportUrl = Url.Action("Index", "BulkImport", new { area = "", module = "lecturer-busy-slot" })
            };

            return View(vm);
        }



        [HttpGet]
        public async Task<IActionResult> SearchUsers(string? keyword)
        {
            var paged = await _userService.GetPagedAsync(null, null, null, null, null, 1, 1000);
            var users = paged.Items.AsEnumerable();

            // Chỉ lấy 3 role được phép hiện trong BusySlot
            users = users.Where(x =>
                x.RoleName == "Giảng viên").ToList();

            if (User.IsInRole("Giảng viên"))
            {
                var currentUserId = GetCurrentUserId();
                users = users.Where(x => x.Id == currentUserId);
            }
            else if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                var currentFacultyId = await GetCurrentFacultyIdAsync();
                users = users.Where(x => x.FacultyId == currentFacultyId).ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                users = users.Where(x =>
                    (x.FullName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            return Json(users.Select(x => new
            {
                id = x.Id,
                name = x.FullName ?? x.UserName
            }));
        }

        public async Task<IActionResult> GetList(
            string? keyword,
            int? userId,
            int? facultyId,
            int? academyYearId,
            int? semesterId,
            int? examPeriodId,
            int? examSessionId,
            int? examSlotId,
            DateOnly? fromDate,
            DateOnly? toDate,
            int page = 1,
            int pageSize = 5)
        {
            ViewBag.ShowActionColumn = User.IsInRole("Giảng viên");
            var filter = await BuildScopeFilter(
                keyword, userId, facultyId, academyYearId, semesterId,
                examPeriodId, examSessionId, examSlotId, fromDate, toDate);

            var result = await _service.GetPagedAsync(filter, page, pageSize);
            return PartialView("_BusySlotTable", result);
        }

        [Authorize(Roles = "Giảng viên")]
        public IActionResult Create()
        {
            return View(new LecturerBusySlotDto
            {
                BusyDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }

        [HttpPost]
        [Authorize(Roles = "Giảng viên")]
        public async Task<IActionResult> Create(LecturerBusySlotDto dto)
        {
            dto.UserId = GetCurrentUserId();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Đăng ký lịch bận thành công!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View(dto);
            }
        }

        [Authorize(Roles = "Giảng viên")]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            if (!CanLecturerEdit(data)) return Forbid();

            return View(data);
        }

        [HttpPost]
        [Authorize(Roles = "Giảng viên")]
        public async Task<IActionResult> Edit(LecturerBusySlotDto dto)
        {
            dto.UserId = GetCurrentUserId();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật lịch bận thành công!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View(dto);
            }
        }

        [Authorize(Roles = "Giảng viên")]
        public async Task<IActionResult> Delete(int id)
        {
            var current = await _service.GetByIdAsync(id);
            if (current == null) return NotFound();
            if (!CanLecturerEdit(current)) return Forbid();

            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xoá thành công!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View();
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            if (!await CanViewAsync(data)) return Forbid();

            return View(data);
        }

        private async Task<LecturerBusySlotSearchDto> BuildScopeFilter(
            string? keyword,
            int? userId,
            int? facultyId,
            int? academyYearId,
            int? semesterId,
            int? examPeriodId,
            int? examSessionId,
            int? examSlotId,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            var filter = new LecturerBusySlotSearchDto
            {
                Keyword = keyword,
                UserId = userId,
                FacultyId = facultyId,
                AcademyYearId = academyYearId,
                SemesterId = semesterId,
                ExamPeriodId = examPeriodId,
                ExamSessionId = examSessionId,
                ExamSlotId = examSlotId,
                FromDate = fromDate,
                ToDate = toDate
            };

            if (User.IsInRole("Giảng viên"))
            {
                filter.UserId = GetCurrentUserId();
                filter.FacultyId = null;
            }
            else if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                filter.UserId = null;
                filter.FacultyId = await GetCurrentFacultyIdAsync();
            }
            else if (User.IsInRole("Admin"))
            {
                filter.UserId = null;
                filter.FacultyId = null;
            }
            return filter;
        }

        private bool CanLecturerEdit(LecturerBusySlotDto dto)
        {
            var currentUserId = GetCurrentUserId();
            return currentUserId.HasValue && dto.UserId == currentUserId.Value;
        }
        private async Task<bool> CanViewAsync(LecturerBusySlotDto dto)
        {
            if (User.IsInRole("Admin")) return true;
            if (User.IsInRole("Giảng viên")) return CanLecturerEdit(dto);

            if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                var facultyId = await GetCurrentFacultyIdAsync();
                return facultyId.HasValue && dto.FacultyId == facultyId.Value;
            }

            return false;
        }
    }
}
