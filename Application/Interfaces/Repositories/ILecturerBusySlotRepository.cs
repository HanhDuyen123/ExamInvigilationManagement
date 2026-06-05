using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface ILecturerBusySlotRepository
    {
        Task<PagedResult<LecturerBusySlotDto>> GetPagedAsync(LecturerBusySlotSearchDto filter, int page, int pageSize);
        Task<LecturerBusySlotDto?> GetByIdAsync(int id);

        Task AddAsync(LecturerBusySlot entity);
        Task AddRangeAsync(List<LecturerBusySlot> entities);
        Task UpdateAsync(LecturerBusySlot entity);
        Task DeleteAsync(int id);
        Task AddBusyPeriodAsync(int userId, int periodId, string note);
        Task<bool> BusyPeriodExistsAsync(int userId, int periodId);
        Task ApproveAsync(int id, int approverId);
        Task RejectAsync(int id, int approverId, string reason);
        Task<PagedResult<LecturerPeriodAvailabilityDto>> GetAvailabilityPagedAsync(LecturerPeriodAvailabilitySearchDto filter, int page, int pageSize);
        Task<List<int>> GetDeanIdsForLecturerAsync(int lecturerUserId, CancellationToken cancellationToken = default);
        Task<string> GetLecturerDisplayNameAsync(int lecturerUserId, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            int userId,
            int slotId,
            DateOnly busyDate,
            int? ignoreId = null);
    }
}
