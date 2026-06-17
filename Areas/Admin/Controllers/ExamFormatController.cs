using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class ExamFormatController : Controller
    {
        private readonly IExamFormatService _service;

        public ExamFormatController(IExamFormatService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View(new CrudIndexViewModel
            {
                Title = "Hình thức thi",
                Subtitle = "Quản lý các hình thức thi dùng khi lập lịch và phân công giám thị.",
                CreateUrl = Url.Action(nameof(Create), "ExamFormat", new { area = "Admin" }),
                SearchPartialView = "_ExamFormatSearch"
            });
        }

        public async Task<IActionResult> GetList(string? keyword, int page = 1, int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(keyword, page, pageSize);
            return PartialView("_ExamFormatTable", result);
        }

        [HttpGet]
        public IActionResult Create() => View(new ExamFormatDto { IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamFormatDto dto)
        {
            Normalize(dto);
            if (!ModelState.IsValid) return View(dto);

            if (await _service.CodeExistsAsync(dto.Code))
            {
                ModelState.AddModelError(nameof(dto.Code), "Mã hình thức thi đã tồn tại.");
                return View(dto);
            }

            await _service.CreateAsync(dto);
            TempData.SetNotification("success", "Tạo hình thức thi thành công.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
            {
                TempData.SetNotification("error", "Không tìm thấy hình thức thi cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExamFormatDto dto)
        {
            Normalize(dto);
            if (!ModelState.IsValid) return View(dto);

            var item = await _service.GetByIdAsync(dto.Id);
            if (item == null)
            {
                TempData.SetNotification("error", "Không tìm thấy hình thức thi cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            if (await _service.CodeExistsAsync(dto.Code, dto.Id))
            {
                ModelState.AddModelError(nameof(dto.Code), "Mã hình thức thi đã tồn tại.");
                return View(dto);
            }

            await _service.UpdateAsync(dto);
            TempData.SetNotification("success", "Cập nhật hình thức thi thành công.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
            {
                TempData.SetNotification("error", "Không tìm thấy hình thức thi cần xóa.");
                return RedirectToAction(nameof(Index));
            }

            if (await _service.IsUsedInScheduleAsync(id))
            {
                TempData.SetNotification("error", "Không thể xóa hình thức thi đã được dùng trong lịch thi.");
                return RedirectToAction(nameof(Index));
            }

            await _service.DeleteAsync(id);
            TempData.SetNotification("success", "Xóa hình thức thi thành công.");
            return RedirectToAction(nameof(Index));
        }

        private static void Normalize(ExamFormatDto dto)
        {
            dto.Code = (dto.Code ?? string.Empty).Trim().ToUpperInvariant();
            dto.Name = (dto.Name ?? string.Empty).Trim();
        }
    }
}
