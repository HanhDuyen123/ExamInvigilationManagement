using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Application.Services;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class SlotController : Controller
    {
        private readonly ISlotService _service;

        public SlotController(ISlotService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetByIdAsync(id);
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> GetBySession(int sessionId)
        {
            var data = await _service.GetAllBySessionAsync(sessionId);
            return Json(data.Select(x => new
            {
                id = x.Id,
                name = x.Name
            }));
        }
        [HttpPost]
        public async Task<IActionResult> Add(int sessionId, string name, TimeOnly timeStart, int academyYearId)
        {
            try
            {
                await _service.AddAsync(sessionId, name, timeStart);
                TempData.SetNotification("success", "Đã thêm ca thi.");
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SlotDto dto, int academyYearId)
        {
            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Đã cập nhật ca thi.");
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
            TempData.SetNotification("success", "Đã xóa ca thi.");
            return RedirectToAction("Edit", "AcademyYear", new { id = academyYearId });
        }

        //[HttpPost]
        //public async Task<IActionResult> Update(int id, string name, TimeOnly timeStart, int academyYearId)
        //{
        //    await _service.UpdateAsync(id, name, timeStart);

        //    return RedirectToAction("Detail", "AcademyYear", new { id = academyYearId });
        //}

        //public async Task<IActionResult> Delete(int id)
        //{
        //    await _service.DeleteAsync(id);
        //    return RedirectToAction("Index", "AcademyYear");
        //}
    }
}
