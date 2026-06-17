using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Services;

public class ExamFormatService : IExamFormatService
{
    private readonly IExamFormatRepository _repository;

    public ExamFormatService(IExamFormatRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<ExamFormatDto>> GetPagedAsync(string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return _repository.GetPagedAsync(keyword, page, pageSize, cancellationToken);
    }

    public Task<ExamFormatDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(string code, int? ignoredId = null, CancellationToken cancellationToken = default)
    {
        return _repository.CodeExistsAsync(code, ignoredId, cancellationToken);
    }

    public Task<bool> IsUsedInScheduleAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.IsUsedInScheduleAsync(id, cancellationToken);
    }

    public Task CreateAsync(ExamFormatDto dto, CancellationToken cancellationToken = default)
    {
        return _repository.CreateAsync(dto, cancellationToken);
    }

    public Task UpdateAsync(ExamFormatDto dto, CancellationToken cancellationToken = default)
    {
        return _repository.UpdateAsync(dto, cancellationToken);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.DeleteAsync(id, cancellationToken);
    }
}
