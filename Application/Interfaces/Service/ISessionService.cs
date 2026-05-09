using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface ISessionService
    {
        Task<List<SessionDto>> GetAllByPeriodAsync(int periodId);

        Task AddAsync(int periodId, string name);
        Task UpdateAsync(SessionDto dto);
        Task DeleteAsync(int id);
    }
}
