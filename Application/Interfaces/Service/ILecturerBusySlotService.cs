using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface ILecturerBusySlotService
    {
        Task<PagedResult<LecturerBusySlotDto>> GetPagedAsync(LecturerBusySlotSearchDto filter, int page, int pageSize);
        Task<LecturerBusySlotDto?> GetByIdAsync(int id);
        Task CreateAsync(LecturerBusySlotDto dto);
        Task<int> CreateManyAsync(LecturerBusySlotDto dto);
        Task UpdateAsync(LecturerBusySlotDto dto);
        Task DeleteAsync(int id);
        Task ApproveAsync(int id, int approverId);
        Task RejectAsync(int id, int approverId, string reason);
        Task NotifyBusyRegistrationAsync(LecturerBusySlotDto dto, int createdCount, CancellationToken cancellationToken = default);
        Task<PagedResult<LecturerPeriodAvailabilityDto>> GetAvailabilityPagedAsync(LecturerPeriodAvailabilitySearchDto filter, int page, int pageSize);
        Task SetPeriodAvailabilityAsync(int userId, int periodId, bool isAvailable, int currentUserId, int? facultyScopeId = null);
    }
}
