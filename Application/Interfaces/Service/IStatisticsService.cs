using ExamInvigilationManagement.Application.DTOs.Statistics;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IStatisticsService
    {
        Task<StatisticsDashboardDto> GetDashboardAsync(int userId, string roleName, StatisticsFilterDto filter, CancellationToken cancellationToken = default);
        byte[] ExportPdf(StatisticsDashboardDto dashboard);
        byte[] ExportCsv(StatisticsDashboardDto dashboard);
    }
}
