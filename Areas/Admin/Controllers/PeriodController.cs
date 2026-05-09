using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class PeriodController : Controller
    {
        private readonly IPeriodService _service;

        public PeriodController(IPeriodService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetBySemester(int semesterId)
        {
            var data = await _service.GetAllBySemesterAsync(semesterId);
            return Json(data.Select(x => new
            {
                id = x.Id,
                name = x.Name
            }));
        }

        [HttpPost]
        public async Task<IActionResult> Add(int semesterId, string name, int academyYearId)
        {
            try
            {
                await _service.AddAsync(semesterId, name);
                TempData.SetNotification("success", "Đã thêm đợt thi.");
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PeriodDto dto, int academyYearId)
        {
            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Đã cập nhật đợt thi.");
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
            TempData.SetNotification("success", "Đã xóa đợt thi.");
            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }
    }
}
