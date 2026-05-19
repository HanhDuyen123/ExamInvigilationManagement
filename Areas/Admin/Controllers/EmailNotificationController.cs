using ExamInvigilationManagement.Application.DTOs.Admin.EmailNotification;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmailNotificationController : Controller
    {
        private readonly ApplicationDbContext _db;

        public EmailNotificationController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new CrudIndexViewModel
            {
                Title = "Nhật ký email",
                Subtitle = "Tra soát các email hệ thống đã gửi, trạng thái gửi và lỗi phát sinh khi cần kiểm chứng.",
                SearchPartialView = "_EmailNotificationSearch",
                TableClass = "full-width",
                ShowCreateButton = false
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string? keyword, CancellationToken cancellationToken = default)
        {
            var query = _db.Users
                .AsNoTracking()
                .Include(x => x.Information)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(x =>
                    x.UserName.Contains(kw) ||
                    x.Information.FirstName.Contains(kw) ||
                    x.Information.LastName.Contains(kw));
            }

            var users = await query
                .OrderBy(x => x.UserName)
                .Take(20)
                .Select(x => new
                {
                    id = x.UserId,
                    name = x.UserName + " - " + (x.Information.LastName + " " + x.Information.FirstName).Trim()
                })
                .ToListAsync(cancellationToken);

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            string? keyword,
            int? userId,
            int? facultyId,
            string? status,
            string? type,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 5,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? 5 : pageSize;

            var query = _db.EmailNotifications
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(x => x.Information)
                .Include(x => x.User)
                    .ThenInclude(x => x.Faculty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(x =>
                    x.Email.Contains(kw) ||
                    (x.Type != null && x.Type.Contains(kw)) ||
                    (x.ErrorMessage != null && x.ErrorMessage.Contains(kw)) ||
                    x.User.UserName.Contains(kw) ||
                    x.User.Information.FirstName.Contains(kw) ||
                    x.User.Information.LastName.Contains(kw));
            }

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId.Value);
            if (facultyId.HasValue)
                query = query.Where(x => x.User.FacultyId == facultyId.Value);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(x => x.Type == type);
            if (fromDate.HasValue)
                query = query.Where(x => x.SentAt >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(x => x.SentAt < toDate.Value.Date.AddDays(1));

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(x => x.SentAt)
                .ThenByDescending(x => x.EmailId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EmailNotificationDto
                {
                    Id = x.EmailId,
                    UserId = x.UserId,
                    UserName = x.User.UserName,
                    FullName = (x.User.Information.LastName + " " + x.User.Information.FirstName).Trim(),
                    FacultyName = x.User.Faculty != null ? x.User.Faculty.FacultyName : null,
                    Email = x.Email,
                    Status = x.Status,
                    SentAt = x.SentAt,
                    ErrorMessage = x.ErrorMessage,
                    Type = x.Type
                })
                .ToListAsync(cancellationToken);

            return PartialView("_EmailNotificationTable", new PagedResult<EmailNotificationDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var item = await _db.EmailNotifications
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(x => x.Information)
                .Include(x => x.User)
                    .ThenInclude(x => x.Faculty)
                .Where(x => x.EmailId == id)
                .Select(x => new EmailNotificationDto
                {
                    Id = x.EmailId,
                    UserId = x.UserId,
                    UserName = x.User.UserName,
                    FullName = (x.User.Information.LastName + " " + x.User.Information.FirstName).Trim(),
                    FacultyName = x.User.Faculty != null ? x.User.Faculty.FacultyName : null,
                    Email = x.Email,
                    Status = x.Status,
                    SentAt = x.SentAt,
                    ErrorMessage = x.ErrorMessage,
                    Type = x.Type
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (item == null) return NotFound();
            return View(item);
        }
    }
}
