using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface ISessionRepository
    {
        Task<List<ExamSession>> GetAllByPeriodAsync(int periodId);
        Task<ExamSession?> GetByIdAsync(int id);
        Task AddAsync(int periodId, ExamSession entity);
        Task UpdateAsync(int id, string name);
        Task DeleteAsync(int id);
    }
}
