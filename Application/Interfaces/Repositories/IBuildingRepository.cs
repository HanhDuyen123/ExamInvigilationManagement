using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IBuildingRepository
    {
        Task<List<Building>> GetAllAsync();
        Task<Building?> GetByIdAsync(string id);

        Task<bool> ExistsByIdAsync(string id);
        Task<bool> ExistsByNameAsync(string name, string? excludeId = null);
        Task<bool> HasRoomsAsync(string id);

        Task AddAsync(Building entity);
        Task UpdateAsync(Building entity);
        Task DeleteAsync(string id);
    }
}