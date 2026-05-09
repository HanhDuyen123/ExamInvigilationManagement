using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Domain.Enums;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface ISemesterService
    {
        Task<List<SemesterDto>> GetAllAsync();
        Task AddAsync(int academyYearId, SemesterType type);
        Task UpdateAsync(SemesterDto dto);
        Task DeleteAsync(int id);
    }
}
