using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IPeriodService
    {
        Task<List<PeriodDto>> GetAllBySemesterAsync(int semesterId);
        Task AddAsync(int semesterId, string name);
        Task UpdateAsync(PeriodDto dto);
        Task DeleteAsync(int id);
    }
}
