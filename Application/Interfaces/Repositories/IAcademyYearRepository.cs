using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IAcademyYearRepository
    {
        Task<List<AcademyYear>> GetAllAsync();
        Task<AcademyYear?> GetByIdAsync(int id);
        Task<AcademyYear?> GetByNameAsync(string name);
        Task<AcademyYearDetailDto?> GetDetailAsync(int id);
        Task AddAsync(AcademyYear entity);
        Task UpdateAsync(AcademyYear entity);
        Task DeleteAsync(int id);
    }
}