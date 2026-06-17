using ExamInvigilationManagement.Application.DTOs.Dashboard;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Constants;

namespace ExamInvigilationManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repository;

    public DashboardService(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync(string roleName, int userId, CancellationToken cancellationToken = default)
    {
        var metrics = new DashboardMetricsDto { RoleName = roleName };

        if (string.Equals(roleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            metrics.Admin = await _repository.GetAdminMetricsAsync(cancellationToken);
        }
        else if (string.Equals(roleName, RoleNames.Secretary, StringComparison.OrdinalIgnoreCase) && userId > 0)
        {
            var facultyId = await _repository.GetUserFacultyIdAsync(userId, cancellationToken);
            if (facultyId.HasValue)
                metrics.Secretary = await _repository.GetSecretaryMetricsAsync(facultyId.Value, cancellationToken);
        }
        else if (string.Equals(roleName, RoleNames.Dean, StringComparison.OrdinalIgnoreCase) && userId > 0)
        {
            metrics.Dean = await _repository.GetDeanMetricsAsync(userId, cancellationToken);
        }
        else if (string.Equals(roleName, RoleNames.Lecturer, StringComparison.OrdinalIgnoreCase) && userId > 0)
        {
            metrics.Lecturer = await _repository.GetLecturerMetricsAsync(userId, cancellationToken);
        }

        return metrics;
    }
}
