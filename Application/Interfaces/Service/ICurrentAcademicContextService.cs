using ExamInvigilationManagement.Application.DTOs.Statistics;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface ICurrentAcademicContextService
    {
        Task<CurrentAcademicContextDto?> GetCurrentContextAsync(int userId, string roleName, int? facultyId = null, CancellationToken cancellationToken = default);
    }
}
