using ExamInvigilationManagement.Application.DTOs.Admin.Outbox;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task<PagedResult<OutboxMessageDto>> GetPagedAsync(string? keyword, string? status, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<OutboxMessageDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> RetryAsync(long id, CancellationToken cancellationToken = default);
}
