using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetAllAsync();
        Task<Role?> GetByIdAsync(byte id);

        Task<bool> ExistsByNameAsync(string name, byte? excludeId = null);
        Task<bool> HasUsersAsync(byte id);

        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(byte id);
    }
}