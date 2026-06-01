using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class AutoAssignmentRepository : IAutoAssignmentRepository
    {
        private readonly ApplicationDbContext _db;

        public AutoAssignmentRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int?> GetUserFacultyIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.FacultyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<AutoAssignScheduleDto>> GetSchedulesAsync(
            int semesterId,
            int periodId,
            int facultyId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ExamSchedules
                .AsNoTracking()
                .Where(x =>
                    x.SemesterId == semesterId &&
                    x.PeriodId == periodId &&
                    x.Offering.Subject.FacultyId == facultyId)
                .Select(x => new AutoAssignScheduleDto
                {
                    ExamScheduleId = x.ExamScheduleId,
                    SlotId = x.SlotId,
                    SlotName = x.Slot.SlotName,
                    TimeStart = x.Slot.TimeStart,

                    AcademyYearId = x.AcademyYearId,
                    SemesterId = x.SemesterId,
                    PeriodId = x.PeriodId,
                    SessionId = x.SessionId,

                    RoomId = x.RoomId,
                    RoomDisplay = x.Room.BuildingId + "-" + x.Room.RoomName,

                    OfferingId = x.OfferingId,
                    OfferingUserId = x.Offering.UserId,
                    OfferingFacultyId = x.Offering.Subject.FacultyId,

                    SubjectId = x.Offering.SubjectId,
                    SubjectName = x.Offering.Subject.SubjectName,
                    ClassName = x.Offering.ClassName,
                    GroupNumber = x.Offering.GroupNumber,
                    ExamFormatDisplay = x.ExamFormat != null
                        ? x.ExamFormat.Code + " - " + x.ExamFormat.Name
                        : string.Empty,

                    ExamDate = x.ExamDate,
                    Status = x.Status
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AutoAssignLecturerDto>> GetActiveLecturersAsync(
            int facultyId,
            IEnumerable<string> subjectIds,
            IEnumerable<int> ownerUserIds,
            CancellationToken cancellationToken = default)
        {
            var subjectIdList = subjectIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var ownerIdList = ownerUserIds.Distinct().ToList();
            var subjectLecturerIds = await _db.CourseOfferings
                .AsNoTracking()
                .Where(x => subjectIdList.Contains(x.SubjectId) && x.User.IsActive && x.User.Role.RoleName == "Giảng viên")
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return await _db.Users
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    (x.FacultyId == facultyId || ownerIdList.Contains(x.UserId) || subjectLecturerIds.Contains(x.UserId)) &&
                    x.Role.RoleName != "Admin")
                .Select(x => new AutoAssignLecturerDto
                {
                    UserId = x.UserId,
                    InformationId = x.InformationId,
                    UserName = x.UserName,
                    FullName = x.Information.LastName + " " + x.Information.FirstName,
                    FacultyId = x.FacultyId,
                    FacultyName = x.Faculty != null ? x.Faculty.FacultyName : string.Empty,
                    RoleName = x.Role.RoleName,
                    IsActive = x.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetLecturerLoadsAsync(
            int semesterId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default)
        {
            var userIdList = userIds.Distinct().ToList();
            return await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.ExamSchedule.SemesterId == semesterId &&
                    userIdList.Contains(x.AssigneeId) &&
                    x.Assignee.IsActive &&
                    x.Status != "Từ chối" &&
                    (x.InvigilatorResponses
                        .Where(r => r.UserId == x.AssigneeId)
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault() ?? string.Empty) != "Từ chối")
                .GroupBy(x => x.AssigneeId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
        }

        public async Task<Dictionary<string, HashSet<int>>> GetSubjectLecturerMapAsync(
            IEnumerable<string> subjectIds,
            CancellationToken cancellationToken = default)
        {
            var subjectIdList = subjectIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (subjectIdList.Count == 0)
                return new Dictionary<string, HashSet<int>>();

            var rows = await _db.CourseOfferings
                .AsNoTracking()
                .Where(x =>
                    subjectIdList.Contains(x.SubjectId) &&
                    x.User.IsActive &&
                    x.User.Role.RoleName == "Giảng viên")
                .Select(x => new
                {
                    x.SubjectId,
                    x.UserId
                })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(x => x.SubjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.UserId).ToHashSet());
        }

        public async Task<List<AutoAssignBusySlotDto>> GetBusySlotsAsync(
            IEnumerable<int> userIds,
            IEnumerable<int> slotIds,
            IEnumerable<DateOnly> busyDates,
            CancellationToken cancellationToken = default)
        {
            var userIdList = userIds.Distinct().ToList();
            var slotIdList = slotIds.Distinct().ToList();
            var dateList = busyDates.Distinct().ToList();

            return await _db.LecturerBusySlots
                .AsNoTracking()
                .Where(x =>
                    userIdList.Contains(x.UserId) &&
                    slotIdList.Contains(x.SlotId) &&
                    dateList.Contains(x.BusyDate))
                .Select(x => new AutoAssignBusySlotDto
                {
                    UserId = x.UserId,
                    SlotId = x.SlotId,
                    BusyDate = x.BusyDate
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AutoAssignExistingAssignmentDto>> GetExistingAssignmentsAsync(
            IEnumerable<int> examScheduleIds,
            CancellationToken cancellationToken = default)
        {
            var scheduleIdList = examScheduleIds.Distinct().ToList();

            return await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x => scheduleIdList.Contains(x.ExamScheduleId))
                .Select(x => new AutoAssignExistingAssignmentDto
                {
                    ExamInvigilatorId = x.ExamInvigilatorId,
                    ExamScheduleId = x.ExamScheduleId,
                    UserId = x.AssigneeId,
                    InformationId = x.Assignee.InformationId,
                    PositionNo = x.PositionNo,
                    SlotId = x.ExamSchedule.SlotId,
                    ExamDate = x.ExamSchedule.ExamDate,
                    InvigilatorStatus = x.Status,
                    ResponseStatus = x.InvigilatorResponses
                        .Where(r => r.UserId == x.AssigneeId)
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        public async Task SavePlanAsync(
            AutoAssignPlanDto plan,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.Now;
                var correlationId = Guid.NewGuid();
                if (plan.NewInvigilators.Count > 0)
                {
                    var entities = plan.NewInvigilators.Select(x => new Data.Entities.ExamInvigilator
                    {
                        AssigneeId = x.AssigneeId,
                        AssignerId = x.AssignerId,
                        NewAssigneeId = x.NewAssigneeId,
                        ExamScheduleId = x.ExamScheduleId,
                        PositionNo = x.PositionNo,
                        Status = x.Status,
                        CreateAt = x.CreateAt,
                        UpdateAt = x.UpdateAt
                    }).ToList();

                    await _db.ExamInvigilators.AddRangeAsync(entities, cancellationToken);

                    foreach (var item in plan.NewInvigilators)
                    {
                        _db.AssignmentChangeHistories.Add(new Data.Entities.AssignmentChangeHistory
                        {
                            ExamScheduleId = item.ExamScheduleId,
                            OldAssigneeId = null,
                            NewAssigneeId = item.NewAssigneeId ?? item.AssigneeId,
                            PositionNo = item.PositionNo,
                            ChangeType = "AutoAssign",
                            Reason = "Tự động phân công giám thị.",
                            ActorUserId = item.AssignerId,
                            CreatedAt = now,
                            CorrelationId = correlationId
                        });
                    }
                }

                if (plan.ScheduleStatuses.Count > 0)
                {
                    var statusMap = plan.ScheduleStatuses
                        .GroupBy(x => x.ExamScheduleId)
                        .ToDictionary(g => g.Key, g => g.Last().Status);

                    var scheduleIds = statusMap.Keys.ToList();

                    var schedules = await _db.ExamSchedules
                        .Where(x => scheduleIds.Contains(x.ExamScheduleId))
                        .ToListAsync(cancellationToken);

                    foreach (var schedule in schedules)
                    {
                        if (statusMap.TryGetValue(schedule.ExamScheduleId, out var status))
                            schedule.Status = status;
                    }
                }

                var distinctScheduleIds = plan.NewInvigilators
                    .Select(x => x.ExamScheduleId)
                    .Distinct()
                    .ToList();

                _db.AuditLogs.Add(new Data.Entities.AuditLog
                {
                    EventType = "AutoAssignment",
                    EntityName = "AutoAssignment",
                    EntityId = $"Schedules:{distinctScheduleIds.Count}",
                    Action = "SavePlan",
                    ActorUserId = plan.NewInvigilators.FirstOrDefault()?.AssignerId,
                    NewValues = $"Assignments={plan.NewInvigilators.Count};Schedules={plan.ScheduleStatuses.Count};ScheduleIds={string.Join(",", distinctScheduleIds)}",
                    CreatedAt = now,
                    CorrelationId = correlationId,
                    Source = nameof(AutoAssignmentRepository)
                });

                _db.OutboxMessages.Add(new Data.Entities.OutboxMessage
                {
                    Type = "AutoAssignmentSaved",
                    Payload = $"{{\"assignmentCount\":{plan.NewInvigilators.Count},\"scheduleCount\":{plan.ScheduleStatuses.Count},\"recipientIds\":[{plan.NewInvigilators.FirstOrDefault()?.AssignerId ?? 0}],\"createdBy\":{plan.NewInvigilators.FirstOrDefault()?.AssignerId ?? 0}}}",
                    Status = "Pending",
                    RetryCount = 0,
                    CreatedAt = now,
                    CorrelationId = correlationId
                });

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
