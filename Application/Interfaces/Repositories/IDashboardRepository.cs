using ExamInvigilationManagement.Application.DTOs.Dashboard;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories;

public interface IDashboardRepository
{
    Task<int?> GetUserFacultyIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<DashboardAdminMetricsDto> GetAdminMetricsAsync(CancellationToken cancellationToken = default);
    Task<DashboardSecretaryMetricsDto> GetSecretaryMetricsAsync(int facultyId, CancellationToken cancellationToken = default);
    Task<DashboardDeanMetricsDto> GetDeanMetricsAsync(int userId, CancellationToken cancellationToken = default);
    Task<DashboardLecturerMetricsDto> GetLecturerMetricsAsync(int userId, CancellationToken cancellationToken = default);
}
