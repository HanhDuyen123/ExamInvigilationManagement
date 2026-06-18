using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common.Constants;
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
                    SessionName = x.Session.SessionName,

                    RoomId = x.RoomId,
                    RoomDisplay = x.Room.BuildingId == "KHAC" ? x.Room.RoomName : x.Room.BuildingId + "-" + x.Room.RoomName,

                    OfferingId = x.OfferingId,
                    OfferingUserId = x.Offering.UserId,
                    OfferingUserInformationId = x.Offering.User.InformationId,
                    OfferingFacultyId = x.Offering.Subject.FacultyId,

                    SubjectId = x.Offering.SubjectId,
                    SubjectName = x.Offering.Subject.SubjectName,
                    ClassName = x.Offering.ClassName,
                    GroupNumber = x.Offering.GroupNumber,
                    ExamFormatDisplay = x.ExamFormat != null
                        ? x.ExamFormat.Code + " - " + x.ExamFormat.Name
                        : string.Empty,
                    ExamFormatId = x.ExamFormatId,

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
            var ownerPersonKeys = await _db.Users
                .AsNoTracking()
                .Where(x => ownerIdList.Contains(x.UserId))
                .Select(x => x.InformationId > 0 ? x.InformationId : x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var subjectLecturerIds = await _db.CourseOfferings
                .AsNoTracking()
                .Where(x => subjectIdList.Contains(x.SubjectId) && x.User.IsActive && x.User.Role.RoleName == "Giảng viên")
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var subjectLecturerPersonKeys = await _db.CourseOfferings
                .AsNoTracking()
                .Where(x => subjectIdList.Contains(x.SubjectId) && x.User.IsActive && x.User.Role.RoleName == "Giảng viên")
                .Select(x => x.User.InformationId > 0 ? x.User.InformationId : x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var rows = await _db.Users
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    (x.FacultyId == facultyId ||
                     ownerIdList.Contains(x.UserId) ||
                     ownerPersonKeys.Contains(x.InformationId > 0 ? x.InformationId : x.UserId) ||
                     subjectLecturerIds.Contains(x.UserId) ||
                     subjectLecturerPersonKeys.Contains(x.InformationId > 0 ? x.InformationId : x.UserId)) &&
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

            return rows
                .GroupBy(x => x.PersonKey)
                .Select(g => g
                    .OrderBy(x => GetRolePriority(x.RoleName))
                    .ThenByDescending(x => ownerIdList.Contains(x.UserId))
                    .ThenBy(x => x.UserName)
                    .First())
                .ToList();
        }

        public async Task<Dictionary<int, int>> GetLecturerLoadsAsync(
            int semesterId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default)
        {
            var userIdList = userIds.Distinct().ToList();
            var candidatePersonKeys = await GetPersonKeysByUserIdAsync(userIdList, cancellationToken);
            var personKeySet = candidatePersonKeys.Values.ToHashSet();

            var rows = await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.ExamSchedule.SemesterId == semesterId &&
                    personKeySet.Contains((x.NewAssignee != null && x.NewAssignee.InformationId > 0) ? x.NewAssignee.InformationId : (x.Assignee.InformationId > 0 ? x.Assignee.InformationId : x.AssigneeId)) &&
                    x.Assignee.IsActive &&
                    x.Status != "Từ chối" &&
                    x.Status != ExamInvigilatorStatuses.Cancelled &&
                    (x.InvigilatorResponses
                        .Where(r => r.UserId == (x.NewAssigneeId ?? x.AssigneeId))
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault() ?? string.Empty) != "Từ chối")
                .Select(x => new
                {
                    PersonKey = (x.NewAssignee != null && x.NewAssignee.InformationId > 0) ? x.NewAssignee.InformationId : (x.Assignee.InformationId > 0 ? x.Assignee.InformationId : x.AssigneeId)
                })
                .GroupBy(x => x.PersonKey)
                .Select(g => new
                {
                    PersonKey = g.Key,
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            var countByPersonKey = rows.ToDictionary(x => x.PersonKey, x => x.Count);
            return candidatePersonKeys.ToDictionary(
                x => x.Key,
                x => countByPersonKey.TryGetValue(x.Value, out var count) ? count : 0);
        }

        public async Task<HashSet<int>> GetPeriodAvailableLecturerIdsAsync(
            int periodId,
            int facultyId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();
            var candidatePersonKeys = await GetPersonKeysByUserIdAsync(ids, cancellationToken);
            var rows = await _db.LecturerPeriodAvailabilities
                .AsNoTracking()
                .Where(x => x.PeriodId == periodId && x.User.FacultyId == facultyId)
                .Select(x => x.User.InformationId > 0 ? x.User.InformationId : x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (rows.Count == 0) return new HashSet<int>();
            var allowedPersonKeys = rows.ToHashSet();
            return candidatePersonKeys.Where(x => allowedPersonKeys.Contains(x.Value)).Select(x => x.Key).ToHashSet();
        }

        public Task<bool> HasPeriodAvailabilityListAsync(
            int periodId,
            int facultyId,
            CancellationToken cancellationToken = default)
        {
            return _db.LecturerPeriodAvailabilities
                .AsNoTracking()
                .AnyAsync(x => x.PeriodId == periodId && x.User.FacultyId == facultyId, cancellationToken);
        }

        public async Task<HashSet<int>> GetApprovedBusyPeriodLecturerIdsAsync(
            int periodId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();
            var candidatePersonKeys = await GetPersonKeysByUserIdAsync(ids, cancellationToken);
            var personKeySet = candidatePersonKeys.Values.ToHashSet();
            var busyPersonKeys = await _db.LecturerBusyPeriods
                .AsNoTracking()
                .Where(x =>
                    x.PeriodId == periodId &&
                    x.ApprovalStatus == BusyApprovalStatuses.Approved &&
                    personKeySet.Contains(x.User.InformationId > 0 ? x.User.InformationId : x.UserId))
                .Select(x => x.User.InformationId > 0 ? x.User.InformationId : x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var busySet = busyPersonKeys.ToHashSet();
            return candidatePersonKeys.Where(x => busySet.Contains(x.Value)).Select(x => x.Key).ToHashSet();
        }

        public async Task<AutoAssignmentPolicyDto> GetEffectivePolicyAsync(
            int facultyId,
            int semesterId,
            int periodId,
            CancellationToken cancellationToken = default)
        {
            var policy = await _db.AutoAssignmentPolicies
                .AsNoTracking()
                .Include(x => x.Rules)
                .Include(x => x.ExamFormatRules)
                    .ThenInclude(x => x.ExamFormat)
                .Where(x =>
                    x.FacultyId == facultyId &&
                    x.IsActive &&
                    (x.SemesterId == null || x.SemesterId == semesterId) &&
                    (x.PeriodId == null || x.PeriodId == periodId))
                .OrderByDescending(x => x.PeriodId == periodId)
                .ThenByDescending(x => x.SemesterId == semesterId)
                .ThenByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.PolicyId)
                .FirstOrDefaultAsync(cancellationToken);

            if (policy == null)
                return AutoAssignmentPolicyDto.Default();

            var dto = AutoAssignmentPolicyDto.Default();
            dto.PolicyId = policy.PolicyId;
            dto.PolicyName = policy.PolicyName;
            dto.RequiredInvigilatorsPerSchedule = Math.Max(1, (int)policy.RequiredInvigilatorsPerSchedule);
            dto.AllowCrossFaculty = policy.AllowCrossFaculty;
            dto.RequirePeriodAvailabilityIfExists = policy.RequirePeriodAvailabilityIfExists;
            dto.AllowFacultyMemberAsFallback = policy.AllowFacultyMemberAsFallback;
            dto.MaxAssignmentsPerDay = policy.MaxAssignmentsPerDay;
            dto.MaxAssignmentsPerPeriod = policy.MaxAssignmentsPerPeriod;
            dto.MaxAssignmentsPerSlot = Math.Max(1, policy.MaxAssignmentsPerSlot);
            dto.SolverTimeLimitSeconds = Math.Clamp(policy.SolverTimeLimitSeconds, 1, 60);

            foreach (var rule in policy.Rules)
            {
                dto.Rules[rule.RuleCode] = new AutoAssignmentRuleDto
                {
                    RuleCode = rule.RuleCode,
                    RuleName = rule.RuleName,
                    RuleType = rule.RuleType,
                    IsEnabled = rule.IsEnabled,
                    IsRequired = rule.IsRequired,
                    PriorityOrder = rule.PriorityOrder,
                    Weight = rule.Weight,
                    ParametersJson = rule.ParametersJson
                };
            }

            dto.ExamFormatPolicies = policy.ExamFormatRules
                .Where(x => x.ExamFormat != null && x.ExamFormat.IsActive)
                .ToDictionary(
                    x => x.ExamFormatId,
                    x => new AutoAssignmentExamFormatPolicyDto
                    {
                        ExamFormatId = x.ExamFormatId,
                        Code = x.ExamFormat.Code,
                        Name = x.ExamFormat.Name,
                        AssignmentMode = NormalizeAssignmentMode(x.AssignmentMode)
                    });

            return dto;
        }

        private static string NormalizeAssignmentMode(string? value)
        {
            return value switch
            {
                AutoAssignmentExamFormatAssignmentModes.OwnerOnly => AutoAssignmentExamFormatAssignmentModes.OwnerOnly,
                AutoAssignmentExamFormatAssignmentModes.Skip => AutoAssignmentExamFormatAssignmentModes.Skip,
                _ => AutoAssignmentExamFormatAssignmentModes.Full
            };
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
                    PersonKey = x.User.InformationId > 0 ? x.User.InformationId : x.UserId
                })
                .ToListAsync(cancellationToken);

            var selectedUsers = await _db.Users
                .AsNoTracking()
                .Where(x => x.IsActive && x.Role.RoleName != "Admin")
                .Select(x => new
                {
                    x.UserId,
                    PersonKey = x.InformationId > 0 ? x.InformationId : x.UserId
                })
                .ToListAsync(cancellationToken);
            var selectedUserByPersonKey = selectedUsers
                .GroupBy(x => x.PersonKey)
                .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).ToHashSet());

            return rows
                .GroupBy(x => x.SubjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(x => selectedUserByPersonKey.TryGetValue(x.PersonKey, out var ids) ? ids : new HashSet<int>()).ToHashSet());
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
            var candidatePersonKeys = await GetPersonKeysByUserIdAsync(userIdList, cancellationToken);
            var userIdByPersonKey = candidatePersonKeys
                .GroupBy(x => x.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToList());
            var personKeySet = candidatePersonKeys.Values.ToHashSet();

            var rows = await _db.LecturerBusySlots
                .AsNoTracking()
                .Where(x =>
                    personKeySet.Contains(x.User.InformationId > 0 ? x.User.InformationId : x.UserId) &&
                    x.ApprovalStatus == BusyApprovalStatuses.Approved &&
                    slotIdList.Contains(x.SlotId) &&
                    dateList.Contains(x.BusyDate))
                .Select(x => new AutoAssignBusySlotDto
                {
                    UserId = x.User.InformationId > 0 ? x.User.InformationId : x.UserId,
                    SlotId = x.SlotId,
                    BusyDate = x.BusyDate
                })
                .ToListAsync(cancellationToken);

            return rows
                .SelectMany(x => userIdByPersonKey.TryGetValue(x.UserId, out var ids)
                    ? ids.Select(id => new AutoAssignBusySlotDto { UserId = id, SlotId = x.SlotId, BusyDate = x.BusyDate })
                    : Enumerable.Empty<AutoAssignBusySlotDto>())
                .ToList();
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
                    UserId = x.NewAssigneeId ?? x.AssigneeId,
                    InformationId = x.NewAssignee != null ? x.NewAssignee.InformationId : x.Assignee.InformationId,
                    PositionNo = x.PositionNo,
                    SlotId = x.ExamSchedule.SlotId,
                    ExamDate = x.ExamSchedule.ExamDate,
                    InvigilatorStatus = x.Status,
                    ResponseStatus = x.InvigilatorResponses
                        .Where(r => r.UserId == (x.NewAssigneeId ?? x.AssigneeId))
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

                if (plan.CancelledExistingInvigilatorIds.Count > 0)
                {
                    var cancelledIds = plan.CancelledExistingInvigilatorIds.Distinct().ToList();
                    var existingInvigilators = await _db.ExamInvigilators
                        .Where(x => cancelledIds.Contains(x.ExamInvigilatorId))
                        .ToListAsync(cancellationToken);

                    foreach (var existing in existingInvigilators)
                    {
                        existing.Status = ExamInvigilatorStatuses.Cancelled;
                        existing.UpdateAt = now;

                        _db.AssignmentChangeHistories.Add(new Data.Entities.AssignmentChangeHistory
                        {
                            ExamScheduleId = existing.ExamScheduleId,
                            ExamInvigilatorId = existing.ExamInvigilatorId,
                            OldAssigneeId = existing.NewAssigneeId ?? existing.AssigneeId,
                            NewAssigneeId = null,
                            PositionNo = existing.PositionNo,
                            ChangeType = "AutoReassignCancel",
                            Reason = "Hủy phân công cũ do lịch thi bị từ chối duyệt và được chạy phân công lại.",
                            ActorUserId = plan.NewInvigilators.FirstOrDefault()?.AssignerId,
                            CreatedAt = now,
                            CorrelationId = correlationId
                        });
                    }
                }

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

        private async Task<Dictionary<int, int>> GetPersonKeysByUserIdAsync(
            IEnumerable<int> userIds,
            CancellationToken cancellationToken)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<int, int>();

            return await _db.Users
                .AsNoTracking()
                .Where(x => ids.Contains(x.UserId))
                .Select(x => new
                {
                    x.UserId,
                    PersonKey = x.InformationId > 0 ? x.InformationId : x.UserId
                })
                .ToDictionaryAsync(x => x.UserId, x => x.PersonKey, cancellationToken);
        }

        private static int GetRolePriority(string? roleName)
        {
            return roleName switch
            {
                "Giảng viên" => 0,
                "Trưởng khoa" => 1,
                "Thư ký khoa" => 2,
                _ => 3
            };
        }
    }
}
