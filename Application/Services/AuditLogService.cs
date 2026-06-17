using ExamInvigilationManagement.Application.DTOs.Admin.Audit;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<AuditLogDto>> GetPagedAsync(string? keyword, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return _repository.GetPagedAsync(keyword, entityName, action, fromDate, toDate, page, pageSize, cancellationToken);
    }

    public Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }
}
