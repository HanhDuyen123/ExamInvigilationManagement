using ExamInvigilationManagement.Application.DTOs.Admin.Role;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly IRoleService _service;

        public RoleController(IRoleService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var vm = new CrudIndexViewModel
            {
                Title = "Role Management",
                Subtitle = "Quản lý phân quyền hệ thống",
                CreateUrl = Url.Action("Create", "Role", new { area = "Admin" }),
                SearchPartialView = "_RoleSearch"
            };

            return View(vm);
        }

        public async Task<IActionResult> GetList(string? keyword, int page = 1, int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(keyword, page, pageSize);
            return PartialView("_RoleTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? keyword)
        {
            var data = await _service.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data
                    .Where(x => x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(data.Select(x => new
            {
                id = x.Id,
                name = x.Name
            }));
        }

        [HttpGet]
        public IActionResult Create() => View(new RoleDto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo vai trò thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(dto.Name), ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(byte id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy vai trò cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật vai trò thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(dto.Name), ex.Message);
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(byte id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xóa vai trò thành công.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}