using ExamInvigilationManagement.Application.DTOs.Admin.Audit;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AuditLogController : Controller
{
    private readonly ApplicationDbContext _db;

    public AuditLogController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new CrudIndexViewModel
        {
            Title = "Lịch sử hệ thống",
            Subtitle = "Tra cứu thay đổi dữ liệu và các thao tác nghiệp vụ quan trọng.",
            SearchPartialView = "_AuditLogSearch",
            TableClass = "full-width",
            ShowCreateButton = false
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        string? keyword,
        string? entityName,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x => x.EventType.Contains(kw) || x.EntityName.Contains(kw) || (x.EntityId != null && x.EntityId.Contains(kw)) || (x.Note != null && x.Note.Contains(kw)) || (x.Source != null && x.Source.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(x => x.EntityName == entityName);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (fromDate.HasValue) query = query.Where(x => x.CreatedAt >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.CreatedAt < toDate.Value.Date.AddDays(1));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.AuditLogId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.AuditLogId,
                EventType = x.EventType,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                ActorUserId = x.ActorUserId,
                ActorUserName = x.ActorUserId == null ? null : _db.Users.Where(u => u.UserId == x.ActorUserId).Select(u => u.UserName).FirstOrDefault(),
                ActorFullName = x.ActorUserId == null ? null : _db.Users.Where(u => u.UserId == x.ActorUserId).Select(u => (u.Information.LastName + " " + u.Information.FirstName).Trim()).FirstOrDefault(),
                Note = x.Note,
                CorrelationId = x.CorrelationId,
                CreatedAt = x.CreatedAt,
                Source = x.Source
            })
            .ToListAsync(cancellationToken);

        return PartialView("_AuditLogTable", new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken = default)
    {
        var item = await _db.AuditLogs.AsNoTracking()
            .Where(x => x.AuditLogId == id)
            .Select(x => new AuditLogDto
            {
                Id = x.AuditLogId,
                EventType = x.EventType,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                ActorUserId = x.ActorUserId,
                ActorUserName = x.ActorUserId == null ? null : _db.Users.Where(u => u.UserId == x.ActorUserId).Select(u => u.UserName).FirstOrDefault(),
                ActorFullName = x.ActorUserId == null ? null : _db.Users.Where(u => u.UserId == x.ActorUserId).Select(u => (u.Information.LastName + " " + u.Information.FirstName).Trim()).FirstOrDefault(),
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                Note = x.Note,
                CorrelationId = x.CorrelationId,
                CreatedAt = x.CreatedAt,
                Source = x.Source
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item == null ? NotFound() : View(item);
    }
}
