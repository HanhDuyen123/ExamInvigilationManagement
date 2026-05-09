using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IPeriodRepository
    {
        Task<List<ExamPeriod>> GetAllBySemesterAsync(int semesterId);
        Task<ExamPeriod?> GetByIdAsync(int id);
        Task AddAsync(int semesterId, ExamPeriod entity);
        Task UpdateAsync(int id, string name);
        Task DeleteAsync(int id);
    }
}
