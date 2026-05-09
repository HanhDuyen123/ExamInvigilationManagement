using ExamInvigilationManagement.Application.DTOs.Admin.Room;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IRoomService
    {
        Task<PagedResult<RoomDto>> GetPagedAsync(string? keyword, string? buildingId, int page, int pageSize);
        Task<RoomDto?> GetByIdAsync(int id);
        Task<List<RoomDto>> GetAllAsync();
        Task CreateAsync(RoomDto dto);
        Task UpdateAsync(RoomDto dto);
        Task DeleteAsync(int id);
    }
}
