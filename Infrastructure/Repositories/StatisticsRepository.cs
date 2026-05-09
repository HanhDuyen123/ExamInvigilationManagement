using ExamInvigilationManagement.Application.DTOs.Statistics;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class StatisticsRepository : IStatisticsRepository
    {
        private readonly ApplicationDbContext _db;

        public StatisticsRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<StatisticsDashboardDto> GetDashboardAsync(int userId, string roleName, StatisticsFilterDto filter, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Include(x => x.Faculty)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            var isLecturer = roleName.Equals("Giảng viên", StringComparison.OrdinalIgnoreCase);
            var isFacultyScope = roleName.Equals("Thư ký khoa", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Trưởng khoa", StringComparison.OrdinalIgnoreCase);

            var schedules = _db.ExamSchedules.AsNoTracking().AsQueryable();
            if (isFacultyScope && user?.FacultyId != null)
                schedules = schedules.Where(x => x.Offering.Subject.FacultyId == user.FacultyId.Value);
            if (isLecturer)
                schedules = schedules.Where(x => x.ExamInvigilators.Any(i => i.AssigneeId == userId));

            schedules = ApplyScheduleFilter(schedules, filter);

            var invigilators = _db.ExamInvigilators.AsNoTracking().Where(x => schedules.Select(s => s.ExamScheduleId).Contains(x.ExamScheduleId));
            if (isLecturer)
                invigilators = invigilators.Where(x => x.AssigneeId == userId);

            var scheduleRows = await schedules
                .Select(x => new
                {
                    x.ExamScheduleId,
                    x.Status,
                    PeriodName = x.Period.PeriodName,
                    SessionName = x.Session.SessionName,
                    SlotName = x.Slot.SlotName,
                    InvigilatorCount = x.ExamInvigilators.Count
                })
                .ToListAsync(cancellationToken);

            var assignmentRows = await invigilators
                .Select(x => new
                {
                    x.ExamInvigilatorId,
                    x.AssigneeId,
                    x.ExamSchedule.ExamDate,
                    PeriodName = x.ExamSchedule.Period.PeriodName,
                    SessionName = x.ExamSchedule.Session.SessionName,
                    SlotName = x.ExamSchedule.Slot.SlotName,
                    UserName = x.Assignee.UserName,
                    LastName = x.Assignee.Information.LastName,
                    FirstName = x.Assignee.Information.FirstName,
                    ResponseStatus = x.InvigilatorResponses
                        .Where(r => r.UserId == x.AssigneeId)
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            var totalSchedules = scheduleRows.Count;
            var approvedSchedules = scheduleRows.Count(x => x.Status == "Đã duyệt");
            var fullCoveredSchedules = scheduleRows.Count(x => x.InvigilatorCount >= 2);
            var totalAssignments = assignmentRows.Count;
            var confirmed = assignmentRows.Count(x => x.ResponseStatus == "Xác nhận");
            var rejected = assignmentRows.Count(x => x.ResponseStatus == "Từ chối");
            var pending = assignmentRows.Count(x => string.IsNullOrWhiteSpace(x.ResponseStatus));

            var dashboard = new StatisticsDashboardDto
            {
                RoleName = roleName,
                ScopeName = isLecturer ? "Lịch coi thi của tôi" : isFacultyScope ? user?.Faculty?.FacultyName ?? "Khoa hiện tại" : "Toàn hệ thống",
                Filter = filter,
                Metrics = BuildMetrics(totalSchedules, approvedSchedules, fullCoveredSchedules, totalAssignments, confirmed, rejected, pending)
            };

            dashboard.ScheduleStatus = scheduleRows
                .GroupBy(x => x.Status)
                .Select(g => new StatisticChartPointDto { Label = string.IsNullOrWhiteSpace(g.Key) ? "Chưa xác định" : g.Key, Value = g.Count() })
                .OrderByDescending(x => x.Value)
                .ToList();
            SetRates(dashboard.ScheduleStatus);

            dashboard.ResponseStatus = BuildResponseStatus(confirmed, rejected, pending);

            dashboard.SchedulesByPeriod = scheduleRows
                .GroupBy(x => x.PeriodName)
                .Select(g => new StatisticChartPointDto { Label = string.IsNullOrWhiteSpace(g.Key) ? "Chưa xác định" : g.Key, Value = g.Count() })
                .OrderByDescending(x => x.Value)
                .Take(8)
                .ToList();
            SetRates(dashboard.SchedulesByPeriod);

            dashboard.RejectionsBySession = assignmentRows
                .Where(x => x.ResponseStatus == "Từ chối")
                .GroupBy(x => BuildGroupLabel(x.SessionName, x.SlotName))
                .Select(g => new StatisticChartPointDto { Label = g.Key, Value = g.Count() })
                .OrderByDescending(x => x.Value)
                .Take(8)
                .ToList();
            SetRates(dashboard.RejectionsBySession);

            dashboard.LecturerWorkloads = assignmentRows
                .GroupBy(x => new { x.AssigneeId, x.UserName, x.LastName, x.FirstName })
                .Select(g => new LecturerWorkloadStatisticDto
                {
                    LecturerId = g.Key.AssigneeId,
                    LecturerName = BuildFullName(g.Key.LastName, g.Key.FirstName, g.Key.UserName),
                    AssignedCount = g.Count(),
                    ConfirmedCount = g.Count(x => x.ResponseStatus == "Xác nhận"),
                    RejectedCount = g.Count(x => x.ResponseStatus == "Từ chối")
                })
                .OrderByDescending(x => x.AssignedCount)
                .Take(isLecturer ? 12 : 10)
                .ToList();

            foreach (var item in dashboard.LecturerWorkloads)
            {
                item.PendingCount = Math.Max(0, item.AssignedCount - item.ConfirmedCount - item.RejectedCount);
                item.ConfirmationRate = item.AssignedCount == 0 ? 0 : Math.Round(item.ConfirmedCount * 100m / item.AssignedCount, 1);
            }

            dashboard.SlotCoverage = scheduleRows
                .GroupBy(x => new { x.PeriodName, x.SessionName, x.SlotName })
                .Select(g => new SlotCoverageStatisticDto
                {
                    PeriodName = string.IsNullOrWhiteSpace(g.Key.PeriodName) ? "Chưa xác định" : g.Key.PeriodName,
                    SessionName = string.IsNullOrWhiteSpace(g.Key.SessionName) ? "Chưa xác định" : g.Key.SessionName,
                    SlotName = string.IsNullOrWhiteSpace(g.Key.SlotName) ? "Chưa xác định" : g.Key.SlotName,
                    ScheduleCount = g.Count(),
                    FullCoveredCount = g.Count(x => x.InvigilatorCount >= 2)
                })
                .OrderByDescending(x => x.ScheduleCount)
                .Take(10)
                .ToList();

            foreach (var item in dashboard.SlotCoverage)
                item.CoverageRate = item.ScheduleCount == 0 ? 0 : Math.Round(item.FullCoveredCount * 100m / item.ScheduleCount, 1);

            if (isLecturer)
            {
                dashboard.LecturerMonthlyWorkload = assignmentRows
                    .GroupBy(x => new { x.ExamDate.Year, x.ExamDate.Month })
                    .Select(g => new LecturerMonthlyStatisticDto
                    {
                        MonthLabel = g.Key.Month + "/" + g.Key.Year,
                        AssignedCount = g.Count(),
                        ConfirmedCount = g.Count(x => x.ResponseStatus == "Xác nhận"),
                        RejectedCount = g.Count(x => x.ResponseStatus == "Từ chối")
                    })
                    .OrderBy(x => x.MonthLabel)
                    .Take(12)
                    .ToList();
            }

            return dashboard;
        }

        private static IQueryable<Data.Entities.ExamSchedule> ApplyScheduleFilter(IQueryable<Data.Entities.ExamSchedule> query, StatisticsFilterDto filter)
        {
            if (filter.AcademyYearId.HasValue)
                query = query.Where(x => x.AcademyYearId == filter.AcademyYearId.Value);
            if (filter.SemesterId.HasValue)
                query = query.Where(x => x.SemesterId == filter.SemesterId.Value);
            if (filter.PeriodId.HasValue)
                query = query.Where(x => x.PeriodId == filter.PeriodId.Value);
            if (filter.FromDate.HasValue)
                query = query.Where(x => x.ExamDate >= filter.FromDate.Value.Date);
            if (filter.ToDate.HasValue)
                query = query.Where(x => x.ExamDate <= filter.ToDate.Value.Date);
            return query;
        }

        private static string BuildFullName(string? lastName, string? firstName, string? fallback)
        {
            var fullName = $"{lastName} {firstName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? fallback ?? "Chưa xác định" : fullName;
        }

        private static string BuildGroupLabel(string? first, string? second)
        {
            var left = string.IsNullOrWhiteSpace(first) ? "Chưa xác định" : first;
            var right = string.IsNullOrWhiteSpace(second) ? "Chưa xác định" : second;
            return left + " - " + right;
        }

        private static List<StatisticMetricDto> BuildMetrics(int totalSchedules, int approvedSchedules, int fullCoveredSchedules, int totalAssignments, int confirmed, int rejected, int pending)
        {
            var coverageRate = totalSchedules == 0 ? 0 : Math.Round(fullCoveredSchedules * 100m / totalSchedules, 1);
            var confirmationRate = totalAssignments == 0 ? 0 : Math.Round(confirmed * 100m / totalAssignments, 1);
            return new List<StatisticMetricDto>
            {
                new() { Label = "Tổng lịch thi", Value = totalSchedules.ToString("N0"), Hint = $"{approvedSchedules:N0} lịch đã duyệt", Icon = "bi-calendar2-week", Tone = "primary" },
                new() { Label = "Phân công", Value = totalAssignments.ToString("N0"), Hint = $"Tỷ lệ phủ đủ GT: {coverageRate}%", Icon = "bi-person-check", Tone = "success" },
                new() { Label = "Đã xác nhận", Value = confirmed.ToString("N0"), Hint = $"Tỷ lệ xác nhận: {confirmationRate}%", Icon = "bi-check2-circle", Tone = "info" },
                new() { Label = "Cần xử lý", Value = (rejected + pending).ToString("N0"), Hint = $"{rejected:N0} từ chối, {pending:N0} chưa phản hồi", Icon = "bi-exclamation-triangle", Tone = "warning" }
            };
        }

        private static List<StatisticChartPointDto> BuildResponseStatus(int confirmed, int rejected, int pending)
        {
            var data = new List<StatisticChartPointDto>
            {
                new() { Label = "Xác nhận", Value = confirmed },
                new() { Label = "Từ chối", Value = rejected },
                new() { Label = "Chưa phản hồi", Value = pending }
            };
            SetRates(data);
            return data;
        }

        private static void SetRates(List<StatisticChartPointDto> items)
        {
            var total = items.Sum(x => x.Value);
            foreach (var item in items)
                item.Rate = total == 0 ? 0 : Math.Round(item.Value * 100m / total, 1);
        }
    }
}
