using ExamInvigilationManagement.Application.DTOs.Admin.Subject;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Controllers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class SubjectController : BaseRoleController
    {
        private readonly ISubjectService _service;
        private readonly IFacultyService _facultyService;
        private readonly ICourseOfferingService _courseOfferingService;

        public SubjectController(
            ISubjectService service,
            IFacultyService facultyService,
            ICourseOfferingService courseOfferingService,
            IAdminUserService userService)
            : base(userService)
        {
            _service = service;
            _facultyService = facultyService;
            _courseOfferingService = courseOfferingService;
        }

        public IActionResult Index()
        {
            var model = new CrudIndexViewModel
            {
                Title = "Subject Management",
                Subtitle = "Quản lý môn học",
                CreateUrl = Url.Action("Create", "Subject", new { area = "Admin" }),
                SearchPartialView = "_SubjectSearch",
                ImportUrl = Url.Action("Index", "BulkImport", new { area = "", module = "subject" })
            };

            return View(model);
        }

        public async Task<IActionResult> GetList(
            string? id,
            string? name,
            byte? credit,
            int? facultyId,
            int page = 1,
            int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(id, name, credit, facultyId, page, pageSize);
            return PartialView("_SubjectTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? keyword)
        {
            var data = await _service.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data.Where(x =>
                    (x.Id ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (x.Name ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(data.Select(x => new
            {
                id = x.Id,
                name = $"{x.Id} - {x.Name}"
            }));
        }

        [HttpGet]
        public async Task<IActionResult> SearchForExamSchedule(string? keyword)
        {
            var currentUserId = GetCurrentUserId();
            var currentFacultyId = await GetCurrentFacultyIdAsync();

            IEnumerable<SubjectDto> subjects;

            if (IsAdmin())
            {
                subjects = await _service.GetAllAsync();
            }
            else if (IsFacultyManager())
            {
                subjects = (await _service.GetAllAsync())
                    .Where(x => x.FacultyId == currentFacultyId);
            }
            else if (IsLecturer())
            {
                var offerings = await _courseOfferingService.GetPagedAsync(
                    null, currentUserId, null, null, null, 1, 1000);

                var subjectIds = offerings.Items
                    .Select(x => x.SubjectId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToHashSet();

                subjects = (await _service.GetAllAsync())
                    .Where(x => subjectIds.Contains(x.Id));
            }
            else
            {
                subjects = Enumerable.Empty<SubjectDto>();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                subjects = subjects.Where(x =>
                    (x.Id ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (x.Name ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(subjects.Select(x => new
            {
                id = x.Id,
                name = $"{x.Id} - {x.Name}"
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Faculties = await _facultyService.GetAllAsync();
            return View(new SubjectDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubjectDto dto)
        {
            ViewBag.Faculties = await _facultyService.GetAllAsync();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo môn học thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy môn học cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Faculties = await _facultyService.GetAllAsync();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, SubjectDto dto)
        {
            ViewBag.Faculties = await _facultyService.GetAllAsync();

            if (!string.Equals(id?.Trim(), dto.Id?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Mã môn học không được thay đổi khi cập nhật.");
                return View(dto);
            }

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật môn học thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xóa môn học thành công.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
