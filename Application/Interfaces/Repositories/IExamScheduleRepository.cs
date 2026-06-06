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
        Task UpdateRoomGroupAsync(int baseScheduleId, ExamSchedule entity, List<int> roomIds);
        Task DeleteAsync(int id);

        Task<bool> ExistsOfferingConflictAsync(int offeringId, int? ignoreId = null);
        Task<bool> ExistsRoomConflictAsync(int roomId, DateTime examDate, int slotId, int? ignoreId = null);
        Task<bool> ExistsRoomConflictAsync(int roomId, DateTime examDate, int slotId, IEnumerable<int> ignoreIds);
        Task<bool> RoomExistsAsync(int roomId);
        Task<bool> ExamFormatExistsAsync(int examFormatId);
        Task<bool> HasInvigilatorsAsync(int id);
        Task<bool> HasInvigilatorsInRoomGroupAsync(int baseScheduleId);
        Task<List<int>> GetScheduleIdsInRoomGroupAsync(int baseScheduleId);
        Task<List<ExamFormatDto>> GetExamFormatsAsync(CancellationToken cancellationToken = default);

        Task<ExamScheduleValidationContextDto?> GetOfferingContextAsync(int offeringId);
        Task<ExamScheduleValidationContextDto?> GetSlotContextAsync(int slotId);
        Task MarkApprovalRequestedAsync(IEnumerable<int> scheduleIds, IEnumerable<int> approverIds, int? requestedById = null, int? facultyId = null, string? note = null, CancellationToken cancellationToken = default);
        Task MarkSupportRequestedAsync(IEnumerable<int> scheduleIds, int requestedById, CancellationToken cancellationToken = default);
    }
}
