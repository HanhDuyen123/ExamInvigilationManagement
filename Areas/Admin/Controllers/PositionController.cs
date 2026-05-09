using ExamInvigilationManagement.Application.DTOs.Admin.Position;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class PositionController : Controller
    {
        private readonly IPositionService _service;

        public PositionController(IPositionService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var vm = new CrudIndexViewModel
            {
                Title = "Position Management",
                Subtitle = "Quản lý chức vụ",
                CreateUrl = Url.Action("Create", "Position", new { area = "Admin" }),
                SearchPartialView = "_PositionSearch"
            };

            return View(vm);
        }

        public async Task<IActionResult> GetList(string? keyword, int page = 1, int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(keyword, page, pageSize);
            return PartialView("_PositionTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? keyword)
        {
            var data = await _service.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data
                    .Where(x => x.PositionName.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(data.Select(x => new
            {
                id = x.PositionId,
                name = x.PositionName
            }));
        }

        [HttpGet]
        public IActionResult Create() => View(new PositionDto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PositionDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo chức vụ thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(dto.PositionName), ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(byte id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy chức vụ cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PositionDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật chức vụ thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(dto.PositionName), ex.Message);
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
                TempData.SetNotification("success", "Xóa chức vụ thành công.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}