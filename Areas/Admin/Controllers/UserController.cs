using ExamInvigilationManagement.Application.DTOs.Admin.User;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Common.Security;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IAdminUserService _service;
        private readonly IRoleService _roleService;
        private readonly IInformationService _infoService;
        private readonly IFacultyService _facultyService;

        public UserController(
            IAdminUserService service,
            IRoleService roleService,
            IInformationService infoService,
            IFacultyService facultyService)
        {
            _service = service;
            _roleService = roleService;
            _infoService = infoService;
            _facultyService = facultyService;
        }

        public IActionResult Index()
        {
            var model = new CrudIndexViewModel
            {
                Title = "Tài khoản người dùng",
                Subtitle = "Thiết lập tài khoản, vai trò và trạng thái đăng nhập cho người dùng.",
                CreateUrl = Url.Action("Create", "User", new { area = "Admin" }),
                SearchPartialView = "_UserSearch",
                TableClass = "full-width",
                ImportUrl = Url.Action("Index", "BulkImport", new { area = "", module = "information-user" })
            };

            return View(model);
        }

        public async Task<IActionResult> GetList(
            string? keyword,
            int? roleId,
            int? informationId,
            int? facultyId,
            string? status,
            int page = 1,
            int pageSize = 5)
        {
            bool? isActive = status switch
            {
                "true" => true,
                "false" => false,
                _ => null
            };

            var result = await _service.GetPagedAsync(
                keyword, roleId, informationId, facultyId, isActive, page, pageSize);

            return PartialView("_UserTable", result);
        }

        [HttpGet]
        [RequireRecentAuthentication]
        public async Task<IActionResult> Create()
        {
            await FillLookupsAsync();
            return View(new UserDto { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRecentAuthentication]
        public async Task<IActionResult> Create(UserDto dto)
        {
            await FillLookupsAsync(dto);

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo tài khoản thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        [RequireRecentAuthentication]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy tài khoản cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            await FillLookupsAsync(data);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRecentAuthentication]
        public async Task<IActionResult> Edit(UserDto dto)
        {
            await FillLookupsAsync(dto);

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật tài khoản thành công.");
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
        [RequireRecentAuthentication]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xóa tài khoản thành công.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SearchLecturer(string? keyword)
        {
            var data = await _service.GetPagedAsync(null, null, null, null, null, 1, 1000);

            var lecturers = data.Items
                .Where(x => string.Equals(x.RoleName, "Giảng viên", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                lecturers = lecturers.Where(x =>
                    (x.FullName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(lecturers.Select(x => new
            {
                id = x.Id,
                fullName = string.IsNullOrWhiteSpace(x.FullName)
                    ? x.UserName
                    : $"{x.UserName} - {x.FullName}"
            }));
        }

        private async Task FillLookupsAsync(UserDto? dto = null)
        {
            var roles = await _roleService.GetAllAsync();
            var infos = await _infoService.GetAllAsync();
            var faculties = await _facultyService.GetAllAsync();

            if (dto == null) return;

            dto.RoleName = roles.FirstOrDefault(x => x.Id == dto.RoleId)?.Name;
            dto.FullName = infos.FirstOrDefault(x => x.Id == dto.InformationId) is { } info
                ? $"{info.LastName} {info.FirstName}"
                : dto.FullName;

            dto.FacultyName = dto.FacultyId.HasValue
                ? faculties.FirstOrDefault(x => x.Id == dto.FacultyId.Value)?.Name
                : null;
        }
    }
}
