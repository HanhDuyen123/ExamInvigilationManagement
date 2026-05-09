using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IInformationRepository
    {
        Task<List<Information>> GetAllAsync();
        Task<Information?> GetByIdAsync(int id);

        Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
        Task<bool> HasUsersAsync(int id);

        Task AddAsync(Information entity);
        Task UpdateAsync(Information entity);
        Task DeleteAsync(int id);
    }
}