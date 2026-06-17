using ExamInvigilationManagement.Application.DTOs.Dashboard;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common.Constants;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _db;

    public DashboardRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<int?> GetUserFacultyIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.FacultyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DashboardAdminMetricsDto> GetAdminMetricsAsync(CancellationToken cancellationToken = default)
    {
        return new DashboardAdminMetricsDto
        {
            MissingInvigilatorSchedules = await _db.ExamSchedules.CountAsync(x => x.Status == ExamScheduleStatuses.MissingInvigilator, cancellationToken),
            FailedOutboxMessages = await _db.OutboxMessages.CountAsync(x => x.Status == "Failed", cancellationToken),
            ActiveUsers = await _db.Users.CountAsync(x => x.IsActive, cancellationToken)
        };
    }

    public async Task<DashboardSecretaryMetricsDto> GetSecretaryMetricsAsync(int facultyId, CancellationToken cancellationToken = default)
    {
        var schedules = _db.ExamSchedules.Where(x => x.Offering.Subject.FacultyId == facultyId);
        var today = DateTime.Today;

        return new DashboardSecretaryMetricsDto
        {
            OverdueAssignSchedules = await schedules.CountAsync(x => (x.Status == ExamScheduleStatuses.WaitingAssign || x.Status == ExamScheduleStatuses.MissingInvigilator) && x.ExamDate.Date <= today.AddDays(3), cancellationToken),
            OverdueApprovalSchedules = await schedules.CountAsync(x => x.Status == ExamScheduleStatuses.PendingApproval && !x.ExamScheduleApprovals.Any(a => a.Status == ExamScheduleStatuses.PendingApproval) && x.ExamDate.Date <= today.AddDays(3), cancellationToken),
            WaitingAssignSchedules = await schedules.CountAsync(x => x.Status == ExamScheduleStatuses.WaitingAssign || x.Status == ExamScheduleStatuses.MissingInvigilator, cancellationToken),
            PendingSendApprovalSchedules = await schedules.CountAsync(x => x.Status == ExamScheduleStatuses.PendingApproval && !x.ExamScheduleApprovals.Any(a => a.Status == ExamScheduleStatuses.PendingApproval), cancellationToken),
            ProposedSubstitutions = await _db.InvigilatorSubstitutions.CountAsync(x => x.Status == InvigilatorSubstitutionStatuses.Proposed && x.ExamInvigilator.ExamSchedule.Offering.Subject.FacultyId == facultyId, cancellationToken)
        };
    }

    public async Task<DashboardDeanMetricsDto> GetDeanMetricsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var pendingApprovals = _db.ExamScheduleApprovals
            .Where(x =>
                x.ApproverId == userId &&
                x.Status == ExamScheduleStatuses.PendingApproval &&
                x.ExamSchedule.Status == ExamScheduleStatuses.PendingApproval);

        return new DashboardDeanMetricsDto
        {
            OverdueApprovals = await pendingApprovals.CountAsync(x => x.ExamSchedule.ExamDate.Date <= today.AddDays(3), cancellationToken),
            PendingApprovals = await pendingApprovals.CountAsync(cancellationToken)
        };
    }

    public async Task<DashboardLecturerMetricsDto> GetLecturerMetricsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var currentInformationId = await _db.Users.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.InformationId)
            .FirstOrDefaultAsync(cancellationToken);

        var pendingQuery = _db.ExamInvigilators.Where(x =>
            x.Status != ExamInvigilatorStatuses.Cancelled &&
            ((x.NewAssigneeId ?? x.AssigneeId) == userId || (currentInformationId.HasValue && (x.NewAssignee != null ? x.NewAssignee.InformationId : x.Assignee.InformationId) == currentInformationId.Value)) &&
            x.ExamSchedule.Status == ExamScheduleStatuses.Approved &&
            !x.InvigilatorResponses.Any(r => r.UserId == userId || (currentInformationId.HasValue && r.User.InformationId == currentInformationId.Value)));

        return new DashboardLecturerMetricsDto
        {
            OverdueResponses = await pendingQuery.CountAsync(x => x.ExamSchedule.ExamDate.Date <= today.AddDays(3), cancellationToken),
            PendingResponses = await pendingQuery.CountAsync(cancellationToken),
            RejectedWithoutSubstitution = await _db.ExamInvigilators.CountAsync(x => x.Status != ExamInvigilatorStatuses.Cancelled && (x.NewAssigneeId ?? x.AssigneeId) == userId && x.InvigilatorResponses.Any(r => r.UserId == userId && r.Status == InvigilatorResponseStatuses.Rejected) && !x.InvigilatorSubstitutions.Any(s => s.UserId == userId), cancellationToken)
        };
    }
}
