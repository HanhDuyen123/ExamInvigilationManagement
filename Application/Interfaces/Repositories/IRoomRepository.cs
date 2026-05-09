using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        Task<List<Room>> GetAllAsync();
        Task<Room?> GetByIdAsync(int id);

        Task<bool> BuildingExistsAsync(string buildingId);
        Task<bool> ExistsByBuildingAndRoomNameAsync(string buildingId, string roomName, int? excludeRoomId = null);
        Task<bool> HasExamSchedulesAsync(int roomId);

        Task AddAsync(Room entity);
        Task UpdateAsync(Room entity);
        Task DeleteAsync(int id);
    }
}