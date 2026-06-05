using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface ISemesterRepository
    {
        Task<List<Semester>> GetAllAsync();
        Task<Semester?> GetByIdAsync(int id);
        Task AddAsync(int academyYearId, Semester entity);
        Task UpdateAsync(int id, string name, DateTime? startDate, DateTime? endDate);
        Task DeleteAsync(int id);
    }
}
