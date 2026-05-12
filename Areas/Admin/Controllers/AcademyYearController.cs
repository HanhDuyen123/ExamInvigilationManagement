using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class AcademyYearController : Controller
    {
        private readonly IAcademyYearService _service;

        public AcademyYearController(IAcademyYearService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var vm = new CrudIndexViewModel
            {
                Title = "Năm học",
                Subtitle = "Sắp xếp năm học, học kỳ, đợt thi và các ca thi đi kèm.",
                CreateUrl = @Url.Action("Create", "AcademyYear", new { area = "Admin" }),
                SearchPartialView = "_AcademyYearSearch",
                TableClass = "full-width"
            };

            return View(vm);
        }

        public async Task<IActionResult> GetList(
            string? keyword,
            int? semesterId,
            int page = 1,
            int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(
                keyword,
                semesterId,
                page,
                pageSize);

            return PartialView("_AcademyYearTable", result);
        }
        [HttpGet]
        public async Task<IActionResult> Search(string? keyword)
        {
            var data = await _service.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                data = data.Where(x =>
                    x.Name.ToLower().Contains(keyword.ToLower()))
                    .ToList();
            }

            return Json(data.Select(x => new
            {
                id = x.Id,
                name = x.Name
            }));
        }
        //public IActionResult Create() => View();
        public IActionResult Create()
        {
            var model = DefaultDataBuilder.Build();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAcademyYearDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData.SetNotification("error", "Vui lòng kiểm tra lại thông tin năm học.");
                return View(dto);
            }

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo năm học thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
                var model = DefaultDataBuilder.Build();
                model.Name = dto.Name;
                model.AutoGenerate = dto.AutoGenerate;
                return View(model);
            }
        }

        public async Task<IActionResult> Detail(int id)
        {
            var data = await _service.GetDetailAsync(id);
            return View(data);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetDetailAsync(id);
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AcademyYearDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData.SetNotification("error", "Vui lòng kiểm tra lại thông tin năm học.");
                var detail = await _service.GetDetailAsync(dto.Id);
                return View(detail);
            }

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật năm học thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
                var detail = await _service.GetDetailAsync(dto.Id);
                return View(detail);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
