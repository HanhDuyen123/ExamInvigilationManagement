using ExamInvigilationManagement.Application.DTOs.Admin.Room;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class RoomController : Controller
    {
        private readonly IRoomService _service;
        private readonly IBuildingService _buildingService;

        public RoomController(IRoomService service, IBuildingService buildingService)
        {
            _service = service;
            _buildingService = buildingService;
        }

        public IActionResult Index()
        {
            var model = new CrudIndexViewModel
            {
                Title = "Room Management",
                Subtitle = "Quản lý phòng học",
                CreateUrl = Url.Action("Create", "Room", new { area = "Admin" }),
                SearchPartialView = "_RoomSearch"
            };

            return View(model);
        }

        public async Task<IActionResult> GetList(string? keyword, string? buildingId, int page = 1, int pageSize = 5)
        {
            buildingId = string.IsNullOrWhiteSpace(buildingId) ? null : buildingId;
            var result = await _service.GetPagedAsync(keyword, buildingId, page, pageSize);
            return PartialView("_RoomTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> SearchByBuilding(string? buildingId, string? keyword)
        {
            var data = await _service.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(buildingId))
                data = data.Where(x => x.BuildingId == buildingId).ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
                data = data.Where(x =>
                    x.RoomName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (x.BuildingName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Json(data.Select(x => new { id = x.RoomId, name = $"{x.BuildingId}.{x.RoomName}" }));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Buildings = await _buildingService.GetAllAsync();
            return View(new RoomDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomDto dto)
        {
            ViewBag.Buildings = await _buildingService.GetAllAsync();

            if (!ModelState.IsValid)
            {
                //TempData.SetNotification("error", GetModelStateMessage());
                return View(dto);
            }

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo phòng thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy phòng cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Buildings = await _buildingService.GetAllAsync();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomDto dto)
        {
            ViewBag.Buildings = await _buildingService.GetAllAsync();

            if (!ModelState.IsValid)
                return View(dto);

            if (id != dto.RoomId)
            {
                TempData.SetNotification("error", "Dữ liệu phòng không hợp lệ.");
                return View(dto);
            }

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật phòng thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
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
                TempData.SetNotification("success", "Xóa phòng thành công.");
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
                : "Vui lòng kiểm tra lại thông tin phòng.";
        }
    }
}