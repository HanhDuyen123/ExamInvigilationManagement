using ExamInvigilationManagement.Application.DTOs.Dashboard;

namespace ExamInvigilationManagement.Application.Interfaces.Service;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync(string roleName, int userId, CancellationToken cancellationToken = default);
}
