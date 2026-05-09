namespace ExamInvigilationManagement.Application.DTOs.Statistics
{
    public class StatisticsFilterDto
    {
        public int? AcademyYearId { get; set; }
        public int? SemesterId { get; set; }
        public int? PeriodId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class StatisticsDashboardDto
    {
        public string RoleName { get; set; } = string.Empty;
        public string ScopeName { get; set; } = "Toàn hệ thống";
        public StatisticsFilterDto Filter { get; set; } = new();
        public List<StatisticMetricDto> Metrics { get; set; } = new();
        public List<StatisticChartPointDto> ScheduleStatus { get; set; } = new();
        public List<StatisticChartPointDto> ResponseStatus { get; set; } = new();
        public List<StatisticChartPointDto> SchedulesByPeriod { get; set; } = new();
        public List<StatisticChartPointDto> RejectionsBySession { get; set; } = new();
        public List<LecturerWorkloadStatisticDto> LecturerWorkloads { get; set; } = new();
        public List<SlotCoverageStatisticDto> SlotCoverage { get; set; } = new();
        public List<LecturerMonthlyStatisticDto> LecturerMonthlyWorkload { get; set; } = new();
    }

    public class StatisticMetricDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-graph-up";
        public string Tone { get; set; } = "primary";
    }

    public class StatisticChartPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public decimal Rate { get; set; }
    }

    public class LecturerWorkloadStatisticDto
    {
        public int LecturerId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public int AssignedCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }
        public decimal ConfirmationRate { get; set; }
    }

    public class SlotCoverageStatisticDto
    {
        public string PeriodName { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string SlotName { get; set; } = string.Empty;
        public int ScheduleCount { get; set; }
        public int FullCoveredCount { get; set; }
        public decimal CoverageRate { get; set; }
    }

    public class LecturerMonthlyStatisticDto
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int AssignedCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int RejectedCount { get; set; }
    }
}
