using ExamInvigilationManagement.Application.DTOs.Admin.Outbox;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Services;

public class OutboxService : IOutboxService
{
    private readonly IOutboxRepository _repository;

    public OutboxService(IOutboxRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<OutboxMessageDto>> GetPagedAsync(string? keyword, string? status, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return _repository.GetPagedAsync(keyword, status, type, fromDate, toDate, page, pageSize, cancellationToken);
    }

    public Task<OutboxMessageDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task<bool> RetryAsync(long id, CancellationToken cancellationToken = default)
    {
        return _repository.RetryAsync(id, cancellationToken);
    }
}
