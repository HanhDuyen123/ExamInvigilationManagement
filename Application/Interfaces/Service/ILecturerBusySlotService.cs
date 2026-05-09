using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface ILecturerBusySlotService
    {
        Task<PagedResult<LecturerBusySlotDto>> GetPagedAsync(LecturerBusySlotSearchDto filter, int page, int pageSize);
        Task<LecturerBusySlotDto?> GetByIdAsync(int id);
        Task CreateAsync(LecturerBusySlotDto dto);
        Task UpdateAsync(LecturerBusySlotDto dto);
        Task DeleteAsync(int id);
    }
}