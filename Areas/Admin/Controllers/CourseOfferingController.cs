using ExamInvigilationManagement.Application.DTOs.Admin.CourseOffering;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Controllers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class CourseOfferingController : BaseRoleController
    {
        private readonly ICourseOfferingService _service;
        private readonly ISubjectService _subjectService;
        private readonly ISemesterService _semesterService;
        private readonly ICurrentAcademicContextService _currentAcademicContextService;

        public CourseOfferingController(
            ICourseOfferingService service,
            ISubjectService subjectService,
            IAdminUserService userService,
            ISemesterService semesterService,
            ICurrentAcademicContextService currentAcademicContextService) : base(userService)
        {
            _service = service;
            _subjectService = subjectService;
            _semesterService = semesterService;
            _currentAcademicContextService = currentAcademicContextService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await SetInitialAcademicContextAsync();
            var model = new CrudIndexViewModel
            {
                Title = "Học phần mở",
                Subtitle = "Theo dõi các lớp học phần đang được tổ chức trong từng học kỳ.",
                CreateUrl = Url.Action("Create", "CourseOffering", new { area = "Admin" }),
                SearchPartialView = "_CourseOfferingSearch",
                TableClass = "full-width",
                ImportUrl = Url.Action("Index", "BulkImport", new { area = "", module = "course-offering" })
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            string? subjectId,
            int? userId,
            int? academyYearId,
            int? semesterType,
            string? className,
            string? groupNumber,
            int page = 1,
            int pageSize = 5)
        {
            if (!academyYearId.HasValue && !semesterType.HasValue)
            {
                var context = await GetCurrentContextAsync();
                academyYearId = context?.AcademyYearId;

                if (context?.SemesterId.HasValue == true)
                {
                    var semester = (await _semesterService.GetAllAsync()).FirstOrDefault(x => x.Id == context.SemesterId.Value);
                    semesterType = semester?.Type.HasValue == true ? (int)semester.Type.Value : null;
                }
            }

            var result = await _service.GetPagedAsync(
                subjectId,
                userId,
                academyYearId,
                semesterType,
                className,
                groupNumber,
                page,
                pageSize);

            return PartialView("_CourseOfferingTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> SearchForExamSchedule(
            string? keyword,
            int? academyYearId,
            int? semesterId)
        {
            if (!academyYearId.HasValue && !semesterId.HasValue)
            {
                var context = await GetCurrentContextAsync();
                academyYearId = context?.AcademyYearId;
                semesterId = context?.SemesterId;
            }

            var data = await _service.GetAllAsync();

            if (User.IsInRole("Giảng viên"))
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    data = data.Where(x => x.UserId == currentUserId.Value).ToList();
                }
                else
                {
                    data = new List<CourseOfferingDto>();
                }
            }
            else if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                var currentFacultyId = await GetCurrentFacultyIdAsync();
                if (currentFacultyId.HasValue)
                {
                    data = data.Where(x => x.FacultyId == currentFacultyId.Value).ToList();
                }
                else
                {
                    data = new List<CourseOfferingDto>();
                }
            }

            if (academyYearId.HasValue)
                data = data.Where(x => x.AcademyYearId == academyYearId.Value).ToList();

            if (semesterId.HasValue)
                data = data.Where(x => x.SemesterId == semesterId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = NormalizeSearch(keyword);
                data = data
                    .Where(x => NormalizeSearch(BuildOfferingLabel(x)).Contains(kw))
                    .ToList();
            }

            return Json(data.Select(x => new
            {
                id = x.Id,
                name = BuildOfferingLabel(x),
                subjectId = x.SubjectId,
                subjectName = x.SubjectName,
                userId = x.UserId,
                userName = x.UserName,
                academyYearId = x.AcademyYearId,
                academyYearName = x.AcademicYearName,
                semesterId = x.SemesterId,
                semesterName = x.SemesterName,
                className = x.ClassName,
                groupNumber = x.GroupNumber
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var context = await GetCurrentContextAsync();
            return View(new CourseOfferingDto
            {
                AcademyYearId = context?.AcademyYearId,
                AcademicYearName = context?.AcademyYearName,
                SemesterId = context?.SemesterId,
                SemesterName = context?.SemesterName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseOfferingDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData.SetNotification("error", "Vui lòng kiểm tra lại dữ liệu nhập vào.");
                    await FillLookupNamesAsync(dto);
                    return View(dto);
                }

                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo học phần mở thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                TempData.SetNotification("error", ex.Message);
                await FillLookupNamesAsync(dto);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy học phần mở cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            await FillLookupNamesAsync(data);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CourseOfferingDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData.SetNotification("error", "Vui lòng kiểm tra lại dữ liệu nhập vào.");
                    await FillLookupNamesAsync(dto);
                    return View(dto);
                }

                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật học phần mở thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                TempData.SetNotification("error", ex.Message);
                await FillLookupNamesAsync(dto);
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xóa học phần mở thành công.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task FillLookupNamesAsync(CourseOfferingDto dto)
        {
            if (dto == null) return;

            var subjects = await _subjectService.GetAllAsync();
            var users = await _userService.GetPagedAsync(null, null, null, null, null, 1, 1000);
            var semesters = await _semesterService.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(dto.SubjectId))
            {
                var subject = subjects.FirstOrDefault(x => x.Id == dto.SubjectId);
                dto.SubjectName = subject?.Name;
            }

            if (dto.UserId.HasValue)
            {
                var user = users.Items.FirstOrDefault(x => x.Id == dto.UserId.Value);
                dto.UserName = user?.FullName;
            }

            if (dto.SemesterId.HasValue)
            {
                var semester = semesters.FirstOrDefault(x => x.Id == dto.SemesterId.Value);
                dto.SemesterName = semester?.Name;
                dto.AcademyYearId ??= semester?.AcademyYearId;
                dto.AcademicYearName = semester?.AcademicYear;
            }
        }

        private static string BuildOfferingLabel(CourseOfferingDto x)
        {
            return string.Join(" - ", new[]
            {
                x.SubjectId,
                x.SubjectName,
                x.UserName,
                x.AcademicYearName,
                x.SemesterName,
                x.ClassName,
                x.GroupNumber
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private static string NormalizeSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var s = value.Trim().ToLowerInvariant();
            s = s.Replace("|", " ")
                 .Replace("-", " ")
                 .Replace(".", " ");

            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            return s;
        }

        private async Task<Application.DTOs.Statistics.CurrentAcademicContextDto?> GetCurrentContextAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return null;

            return await _currentAcademicContextService.GetCurrentContextAsync(currentUserId.Value, "Admin");
        }

        private async Task SetInitialAcademicContextAsync()
        {
            var context = await GetCurrentContextAsync();
            ViewBag.InitialAcademyYearId = context?.AcademyYearId;
            ViewBag.InitialAcademyYearName = context?.AcademyYearName;

            if (context?.SemesterId.HasValue == true)
            {
                var semester = (await _semesterService.GetAllAsync()).FirstOrDefault(x => x.Id == context.SemesterId.Value);
                ViewBag.InitialSemesterType = semester?.Type.HasValue == true ? (int?)semester.Type.Value : null;
            }
        }
    }
}
