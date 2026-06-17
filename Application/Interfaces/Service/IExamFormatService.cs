using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service;

public interface IExamFormatService
{
    Task<PagedResult<ExamFormatDto>> GetPagedAsync(string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ExamFormatDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? ignoredId = null, CancellationToken cancellationToken = default);
    Task<bool> IsUsedInScheduleAsync(int id, CancellationToken cancellationToken = default);
    Task CreateAsync(ExamFormatDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExamFormatDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
