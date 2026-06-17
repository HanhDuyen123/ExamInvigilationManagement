using ExamInvigilationManagement.Application.DTOs.Admin.Outbox;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly ApplicationDbContext _db;

    public OutboxRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<OutboxMessageDto>> GetPagedAsync(string? keyword, string? status, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.OutboxMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x => x.Type.Contains(kw) || x.Payload.Contains(kw) || (x.ErrorMessage != null && x.ErrorMessage.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type);
        if (fromDate.HasValue) query = query.Where(x => x.CreatedAt >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.CreatedAt < toDate.Value.Date.AddDays(1));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Status == "Failed" ? 0 : x.Status == "Pending" ? 1 : 2)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OutboxMessageDto
            {
                Id = x.OutboxMessageId,
                Type = x.Type,
                Status = x.Status,
                RetryCount = x.RetryCount,
                CreatedAt = x.CreatedAt,
                ProcessedAt = x.ProcessedAt,
                ErrorMessage = x.ErrorMessage,
                CorrelationId = x.CorrelationId,
                Payload = x.Payload
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OutboxMessageDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<OutboxMessageDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _db.OutboxMessages.AsNoTracking()
            .Where(x => x.OutboxMessageId == id)
            .Select(x => new OutboxMessageDto
            {
                Id = x.OutboxMessageId,
                Type = x.Type,
                Status = x.Status,
                RetryCount = x.RetryCount,
                CreatedAt = x.CreatedAt,
                ProcessedAt = x.ProcessedAt,
                ErrorMessage = x.ErrorMessage,
                CorrelationId = x.CorrelationId,
                Payload = x.Payload
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> RetryAsync(long id, CancellationToken cancellationToken = default)
    {
        var item = await _db.OutboxMessages.FirstOrDefaultAsync(x => x.OutboxMessageId == id, cancellationToken);
        if (item == null) return false;

        item.Status = "Pending";
        item.RetryCount = 0;
        item.ErrorMessage = null;
        item.ProcessedAt = null;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
