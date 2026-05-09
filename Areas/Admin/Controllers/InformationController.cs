using ExamInvigilationManagement.Application.DTOs.Admin.Information;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class InformationController : Controller
    {
        private readonly IInformationService _service;
        private readonly IPositionService _positionService;

        public InformationController(
            IInformationService service,
            IPositionService positionService)
        {
            _service = service;
            _positionService = positionService;
        }

        public IActionResult Index()
        {
            var model = new CrudIndexViewModel
            {
                Title = "Information Management",
                Subtitle = "Quản lý hồ sơ thông tin cá nhân",
                CreateUrl = Url.Action("Create", "Information", new { area = "Admin" }),
                SearchPartialView = "_InformationSearch",
                TableClass = "full-width",
                ImportUrl = Url.Action("Index", "BulkImport", new { area = "", module = "information-user" })
            };

            return View(model);
        }

        public async Task<IActionResult> GetList(
            string? name,
            string? email,
            string? gender,
            DateTime? dob,
            byte? positionId,
            int page = 1,
            int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(name, email, gender, dob, positionId, page, pageSize);
            return PartialView("_InformationTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? keyword)
        {
            var data = await _service.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data
                    .Where(x =>
                        $"{x.LastName} {x.FirstName}".Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(data.Select(x => new
            {
                id = x.Id,
                fullName = $"{x.LastName} {x.FirstName}"
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Positions = await _positionService.GetAllAsync();
            return View(new InformationDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InformationDto dto)
        {
            ViewBag.Positions = await _positionService.GetAllAsync();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo hồ sơ thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                TempData.SetNotification("error", "Không tìm thấy hồ sơ cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Positions = await _positionService.GetAllAsync();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InformationDto dto)
        {
            ViewBag.Positions = await _positionService.GetAllAsync();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật hồ sơ thành công.");
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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xóa hồ sơ thành công.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
