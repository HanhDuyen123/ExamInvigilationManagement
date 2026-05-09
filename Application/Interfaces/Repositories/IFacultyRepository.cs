using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IFacultyRepository
    {
        Task<List<Faculty>> GetAllAsync();
        Task<Faculty?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> HasUsersAsync(int id);
        Task<bool> HasSubjectsAsync(int id);
        Task AddAsync(Faculty entity);
        Task UpdateAsync(Faculty entity);
        Task DeleteAsync(int id);
    }
}
