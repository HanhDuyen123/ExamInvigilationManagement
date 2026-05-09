using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IPositionRepository
    {
        Task<List<Position>> GetAllAsync();
        Task<Position?> GetByIdAsync(byte id);
        Task<bool> ExistsByNameAsync(string name, byte? excludeId = null);
        Task<bool> HasInformationAsync(byte id);
        Task AddAsync(Position position);
        Task UpdateAsync(Position position);
        Task DeleteAsync(byte id);
    }
}