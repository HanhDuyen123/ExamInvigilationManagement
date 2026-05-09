using ExamInvigilationManagement.Application.DTOs.Statistics;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IStatisticsRepository
    {
        Task<StatisticsDashboardDto> GetDashboardAsync(int userId, string roleName, StatisticsFilterDto filter, CancellationToken cancellationToken = default);
    }
}
