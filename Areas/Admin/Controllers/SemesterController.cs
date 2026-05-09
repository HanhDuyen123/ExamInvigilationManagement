using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Application.Services;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class SemesterController : Controller
    {
        private readonly ISemesterService _service;

        public SemesterController(ISemesterService service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetByYear(int academyYearId, string? keyword)
        {
            var data = await _service.GetAllAsync();

            var result = data
                .Where(x => x.AcademyYearId == academyYearId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                result = result.Where(x => x.Name.Contains(keyword));
            }

            return Json(result.Select(x => new {
                id = x.Id,
                name = x.Name
            }));
        }

        [HttpGet]
        public async Task<IActionResult> SearchSemester(string? keyword)
        {
            var data = await _service.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                data = data.Where(x =>
                    x.Name.Contains(keyword))
                    .ToList();
            }

            return Json(data.Select(x => new
            {
                id = x.Id,
                name = x.Name
            }));
        }
        [HttpPost]
        public async Task<IActionResult> Add(int academyYearId, SemesterType type)
        {
            try
            {
                await _service.AddAsync(academyYearId, type);
                TempData.SetNotification("success", "Đã thêm học kỳ.");
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SemesterDto dto, int academyYearId)
        {
            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Đã cập nhật học kỳ.");
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
            }
            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }

        public async Task<IActionResult> Delete(int id, int academyYearId)
        {
            await _service.DeleteAsync(id);
            TempData.SetNotification("success", "Đã xóa học kỳ.");
            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }
    }
}
