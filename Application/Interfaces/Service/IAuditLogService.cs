using ExamInvigilationManagement.Application.DTOs.Admin.Audit;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(string? keyword, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
