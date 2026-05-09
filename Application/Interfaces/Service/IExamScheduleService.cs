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
        Task MarkApprovalRequestedAsync(IEnumerable<int> scheduleIds, IEnumerable<int> approverIds, string? note = null, CancellationToken cancellationToken = default);
    }
}
