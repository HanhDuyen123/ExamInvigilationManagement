using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IExamScheduleService
    {
        Task<PagedResult<ExamScheduleDto>> GetPagedAsync(ExamScheduleSearchDto filter, int page, int pageSize);
        Task<ExamScheduleDto?> GetByIdAsync(int id);
        Task CreateAsync(ExamScheduleDto dto);
        Task UpdateAsync(ExamScheduleDto dto);
        Task DeleteAsync(int id);
        Task<List<ExamFormatDto>> GetExamFormatsAsync(CancellationToken cancellationToken = default);
        Task MarkApprovalRequestedAsync(IEnumerable<int> scheduleIds, IEnumerable<int> approverIds, int? requestedById = null, int? facultyId = null, string? note = null, CancellationToken cancellationToken = default);
        Task MarkSupportRequestedAsync(IEnumerable<int> scheduleIds, int requestedById, CancellationToken cancellationToken = default);
    }
}
