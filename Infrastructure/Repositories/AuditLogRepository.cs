using ExamInvigilationManagement.Application.DTOs.Admin.Audit;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _db;

    public AuditLogRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(string? keyword, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
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

        return new PagedResult<AuditLogDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _db.AuditLogs.AsNoTracking()
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
    }
}
