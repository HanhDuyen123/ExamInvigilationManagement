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
        Task UpdateAsync(LecturerBusySlot entity);
        Task DeleteAsync(int id);

        Task<bool> ExistsAsync(
            int userId,
            int slotId,
            DateOnly busyDate,
            int? ignoreId = null);
    }
}