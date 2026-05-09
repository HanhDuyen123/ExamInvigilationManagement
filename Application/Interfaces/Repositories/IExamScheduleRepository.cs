using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IExamScheduleRepository
    {
        Task<PagedResult<ExamScheduleDto>> GetPagedAsync(ExamScheduleSearchDto filter, int page, int pageSize);
        Task<ExamScheduleDto?> GetByIdAsync(int id);

        Task AddAsync(ExamSchedule entity);
        Task UpdateAsync(ExamSchedule entity);
        Task DeleteAsync(int id);

        Task<bool> ExistsOfferingConflictAsync(int offeringId, int? ignoreId = null);
        Task<bool> ExistsRoomConflictAsync(int roomId, DateTime examDate, int slotId, int? ignoreId = null);
        Task<bool> RoomExistsAsync(int roomId);

        Task<ExamScheduleValidationContextDto?> GetOfferingContextAsync(int offeringId);
        Task<ExamScheduleValidationContextDto?> GetSlotContextAsync(int slotId);
        Task MarkApprovalRequestedAsync(IEnumerable<int> scheduleIds, IEnumerable<int> approverIds, string? note = null, CancellationToken cancellationToken = default);
    }
}
