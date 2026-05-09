using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class SessionController : Controller
    {
        private readonly ISessionService _service;

        public SessionController(ISessionService service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetByPeriod(int periodId)
        {
            var data = await _service.GetAllByPeriodAsync(periodId);
            return Json(data.Select(x => new
            {
                id = x.Id,
                name = x.Name
            }));
        }
        [HttpPost]
        public async Task<IActionResult> Add(int periodId, string name, int academyYearId)
        {
            try
            {
                await _service.AddAsync(periodId, name);
                TempData.SetNotification("success", "Đã thêm buổi thi.");
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SessionDto dto, int academyYearId)
        {
            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Đã cập nhật buổi thi.");
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
            TempData.SetNotification("success", "Đã xóa buổi thi.");
            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }
    }
}
