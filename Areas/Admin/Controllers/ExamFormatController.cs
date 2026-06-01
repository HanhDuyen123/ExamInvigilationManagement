using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Data.Entities;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class ExamFormatController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ExamFormatController(ApplicationDbContext db)
        {
            _db = db;
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
            var query = _db.ExamFormats.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(kw) || x.Name.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ExamFormatDto { Id = x.ExamFormatId, Code = x.Code, Name = x.Name, IsActive = x.IsActive })
                .ToListAsync();

            return PartialView("_ExamFormatTable", new PagedResult<ExamFormatDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
        }

        [HttpGet]
        public IActionResult Create() => View(new ExamFormatDto { IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamFormatDto dto)
        {
            Normalize(dto);
            if (!ModelState.IsValid) return View(dto);

            if (await _db.ExamFormats.AnyAsync(x => x.Code == dto.Code))
            {
                ModelState.AddModelError(nameof(dto.Code), "Mã hình thức thi đã tồn tại.");
                return View(dto);
            }

            _db.ExamFormats.Add(new ExamFormat { Code = dto.Code, Name = dto.Name, IsActive = dto.IsActive });
            await _db.SaveChangesAsync();
            TempData.SetNotification("success", "Tạo hình thức thi thành công.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.ExamFormats.AsNoTracking().FirstOrDefaultAsync(x => x.ExamFormatId == id);
            if (item == null)
            {
                TempData.SetNotification("error", "Không tìm thấy hình thức thi cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            return View(new ExamFormatDto { Id = item.ExamFormatId, Code = item.Code, Name = item.Name, IsActive = item.IsActive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExamFormatDto dto)
        {
            Normalize(dto);
            if (!ModelState.IsValid) return View(dto);

            var item = await _db.ExamFormats.FirstOrDefaultAsync(x => x.ExamFormatId == dto.Id);
            if (item == null)
            {
                TempData.SetNotification("error", "Không tìm thấy hình thức thi cần chỉnh sửa.");
                return RedirectToAction(nameof(Index));
            }

            if (await _db.ExamFormats.AnyAsync(x => x.ExamFormatId != dto.Id && x.Code == dto.Code))
            {
                ModelState.AddModelError(nameof(dto.Code), "Mã hình thức thi đã tồn tại.");
                return View(dto);
            }

            item.Code = dto.Code;
            item.Name = dto.Name;
            item.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            TempData.SetNotification("success", "Cập nhật hình thức thi thành công.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.ExamFormats.FirstOrDefaultAsync(x => x.ExamFormatId == id);
            if (item == null)
            {
                TempData.SetNotification("error", "Không tìm thấy hình thức thi cần xóa.");
                return RedirectToAction(nameof(Index));
            }

            if (await _db.ExamSchedules.AnyAsync(x => x.ExamFormatId == id))
            {
                TempData.SetNotification("error", "Không thể xóa hình thức thi đã được dùng trong lịch thi.");
                return RedirectToAction(nameof(Index));
            }

            _db.ExamFormats.Remove(item);
            await _db.SaveChangesAsync();
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
