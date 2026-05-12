using ExamInvigilationManagement.Application.DTOs.Admin.Building;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class BuildingController : Controller
    {
        private readonly IBuildingService _service;

        public BuildingController(IBuildingService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var vm = new CrudIndexViewModel
            {
                Title = "Giảng đường",
                Subtitle = "Các khu giảng đường dùng để tổ chức lớp học và kỳ thi.",
                CreateUrl = Url.Action("Create", "Building", new { area = "Admin" }),
                SearchPartialView = "_BuildingSearch"
            };

            return View(vm);
        }

        public async Task<IActionResult> GetList(string? keyword, int page = 1, int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(keyword, page, pageSize);
            return PartialView("_BuildingTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? keyword)
        {
            var data = await _service.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();

                data = data.Where(x =>
                    (x.BuildingName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (x.BuildingId ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(data.Select(x => new
            {
                id = x.BuildingId,
                name = x.BuildingName
            }));
        }

        [HttpGet]
        public IActionResult Create() => View(new BuildingDto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BuildingDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData.SetNotification("error", GetModelStateMessage());
                return View(dto);
            }

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo giảng đường thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy giảng đường cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, BuildingDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData.SetNotification("error", GetModelStateMessage());
                return View(dto);
            }

            if (!string.Equals(id?.Trim(), dto.BuildingId?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                TempData.SetNotification("error", "Không được thay đổi mã giảng đường khi cập nhật.");
                return View(dto);
            }

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật giảng đường thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                Console.WriteLine(ex.Message);
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xóa giảng đường thành công.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        private string GetModelStateMessage()
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return errors.Count > 0
                ? string.Join(" ", errors)
                : "Vui lòng kiểm tra lại thông tin giảng đường.";
        }
    }
}
