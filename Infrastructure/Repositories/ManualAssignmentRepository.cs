using ExamInvigilationManagement.Application.DTOs.ManualAssignment;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common.Constants;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class ManualAssignmentRepository : IManualAssignmentRepository
    {
        private readonly ApplicationDbContext _db;

        public ManualAssignmentRepository(ApplicationDbContext db)
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

        public async Task<ManualAssignmentScheduleDto?> GetScheduleAsync(
            int scheduleId,
            int facultyId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ExamSchedules
                .AsNoTracking()
                .Where(x => x.ExamScheduleId == scheduleId && x.Offering.Subject.FacultyId == facultyId)
                .Select(x => new ManualAssignmentScheduleDto
                {
                    ExamScheduleId = x.ExamScheduleId,
                    SlotId = x.SlotId,
                    SlotName = x.Slot.SlotName,
                    TimeStart = x.Slot.TimeStart,
                    AcademyYearId = x.AcademyYearId,
                    AcademyYearName = x.AcademyYear.AcademyYearName,
                    SemesterId = x.SemesterId,
                    SemesterName = x.Semester.SemesterName,
                    PeriodId = x.PeriodId,
                    PeriodName = x.Period.PeriodName,
                    SessionId = x.SessionId,
                    SessionName = x.Session.SessionName,
                    RoomId = x.RoomId,
                    RoomDisplay = x.Room.BuildingId == "KHAC" ? x.Room.RoomName : x.Room.BuildingId + "-" + x.Room.RoomName,
                    OfferingId = x.OfferingId,
                    OfferingUserId = x.Offering.UserId,
                    OfferingUserInformationId = x.Offering.User.InformationId,
                    OfferingUserName = x.Offering.User.UserName,
                    OfferingUserFullName = x.Offering.User.Information.LastName + " " + x.Offering.User.Information.FirstName,
                    OfferingFacultyId = x.Offering.Subject.FacultyId,
                    SubjectId = x.Offering.SubjectId,
                    SubjectName = x.Offering.Subject.SubjectName,
                    ClassName = x.Offering.ClassName,
                    GroupNumber = x.Offering.GroupNumber,
                    ExamFormatDisplay = x.ExamFormat != null ? x.ExamFormat.Code + " - " + x.ExamFormat.Name : string.Empty,
                    ExamDate = x.ExamDate,
                    Status = x.Status,
                    CurrentInvigilatorCount = x.ExamInvigilators.Count(i =>
                        i.Status != InvigilatorResponseStatuses.Rejected &&
                        (i.InvigilatorResponses
                            .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                            .OrderByDescending(r => r.ResponseAt)
                            .Select(r => r.Status)
                            .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<ManualAssignmentCurrentInvigilatorDto>> GetCurrentInvigilatorsAsync(
            int scheduleId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x => x.ExamScheduleId == scheduleId)
                .OrderBy(x => x.PositionNo)
                .Select(x => new ManualAssignmentCurrentInvigilatorDto
                {
                    ExamInvigilatorId = x.ExamInvigilatorId,
                    UserId = x.AssigneeId,
                    InformationId = x.Assignee.InformationId,
                    UserName = x.Assignee.UserName,
                    FullName = x.Assignee.Information.LastName + " " + x.Assignee.Information.FirstName,
                    NewUserId = x.NewAssigneeId,
                    NewInformationId = x.NewAssignee != null ? x.NewAssignee.InformationId : null,
                    NewUserName = x.NewAssignee != null ? x.NewAssignee.UserName : string.Empty,
                    NewFullName = x.NewAssignee != null ? x.NewAssignee.Information.LastName + " " + x.NewAssignee.Information.FirstName : string.Empty,
                    PositionNo = x.PositionNo,
                    Status = x.Status,
                    ResponseStatus = x.InvigilatorResponses
                        .Where(r => r.UserId == (x.NewAssigneeId ?? x.AssigneeId))
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault() ?? (x.Status == "Chờ xác nhận" ? "Chờ phản hồi" : x.Status),
                    ResponseNote = x.InvigilatorResponses
                        .Where(r => r.UserId == (x.NewAssigneeId ?? x.AssigneeId))
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Note)
                        .FirstOrDefault() ?? string.Empty,
                    ResponseAt = x.InvigilatorResponses
                        .Where(r => r.UserId == (x.NewAssigneeId ?? x.AssigneeId))
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.ResponseAt)
                        .FirstOrDefault(),
                    AssignedAt = x.CreateAt,
                    UpdatedAt = x.UpdateAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ManualAssignmentActivityLogDto>> GetActivityLogsAsync(
            int scheduleId,
            CancellationToken cancellationToken = default)
        {
            var assignmentLogs = await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x => x.ExamScheduleId == scheduleId)
                .Select(x => new ManualAssignmentActivityLogDto
                {
                    OccurredAt = x.CreateAt,
                    Type = "assign",
                    Title = "Phân công giám thị",
                    Description = "Vị trí GT " + x.PositionNo + " được phân công cho " +
                                  (x.Assignee.Information != null ? x.Assignee.Information.LastName + " " + x.Assignee.Information.FirstName : x.Assignee.UserName)
                })
                .ToListAsync(cancellationToken);

            var responseLogs = await _db.InvigilatorResponses
                .AsNoTracking()
                .Where(x => x.ExamInvigilator.ExamScheduleId == scheduleId)
                .Select(x => new ManualAssignmentActivityLogDto
                {
                    OccurredAt = x.ResponseAt,
                    Type = x.Status == "Từ chối" ? "reject" : "confirm",
                    Title = "Phản hồi của giảng viên",
                    Description = (x.User.Information != null ? x.User.Information.LastName + " " + x.User.Information.FirstName : x.User.UserName) +
                                  " đã " + x.Status.ToLower() + " vị trí GT " + x.ExamInvigilator.PositionNo +
                                  (string.IsNullOrWhiteSpace(x.Note) ? string.Empty : ". Ghi chú: " + x.Note)
                })
                .ToListAsync(cancellationToken);

            var substitutionLogs = await _db.InvigilatorSubstitutions
                .AsNoTracking()
                .Where(x => x.ExamInvigilator.ExamScheduleId == scheduleId)
                .Select(x => new ManualAssignmentActivityLogDto
                {
                    OccurredAt = x.Status == "Đã duyệt" ? x.ExamInvigilator.UpdateAt : x.CreateAt,
                    Type = x.Status == "Đã duyệt" ? "replace" : x.Status == "Từ chối duyệt" ? "reject" : "proposal",
                    Title = x.Status == "Đã duyệt" ? "Đổi giám thị" : "Đề xuất thay thế",
                    Description = x.Status == "Đã duyệt"
                        ? "Vị trí GT " + x.ExamInvigilator.PositionNo + " được đổi từ " +
                          (x.User.Information != null ? x.User.Information.LastName + " " + x.User.Information.FirstName : x.User.UserName) +
                          " sang " + (x.SubstituteUser.Information != null ? x.SubstituteUser.Information.LastName + " " + x.SubstituteUser.Information.FirstName : x.SubstituteUser.UserName)
                        : (x.User.Information != null ? x.User.Information.LastName + " " + x.User.Information.FirstName : x.User.UserName) +
                          " đề xuất " + (x.SubstituteUser.Information != null ? x.SubstituteUser.Information.LastName + " " + x.SubstituteUser.Information.FirstName : x.SubstituteUser.UserName) +
                          " thay thế vị trí GT " + x.ExamInvigilator.PositionNo + ". Trạng thái: " + x.Status
                })
                .ToListAsync(cancellationToken);

            return assignmentLogs
                .Concat(responseLogs)
                .Concat(substitutionLogs)
                .Where(x => x.OccurredAt.HasValue)
                .OrderByDescending(x => x.OccurredAt)
                .ToList();
        }

        public async Task<List<ManualAssignmentLecturerOptionDto>> GetActiveLecturersAsync(
            int facultyId,
            string subjectId,
            int ownerUserId,
            CancellationToken cancellationToken = default)
        {
            var ownerInformationId = await _db.Users
                .AsNoTracking()
                .Where(x => x.UserId == ownerUserId)
                .Select(x => x.InformationId)
                .FirstOrDefaultAsync(cancellationToken);

            var subjectLecturerIds = await _db.CourseOfferings
                .AsNoTracking()
                .Where(x => x.SubjectId == subjectId && x.User.IsActive && x.User.Role.RoleName == "Giảng viên")
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var subjectLecturerPersonKeys = await _db.CourseOfferings
                .AsNoTracking()
                .Where(x => x.SubjectId == subjectId && x.User.IsActive && x.User.Role.RoleName == "Giảng viên")
                .Select(x => x.User.InformationId > 0 ? x.User.InformationId : x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var rows = await _db.Users
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    (x.FacultyId == facultyId ||
                     x.UserId == ownerUserId ||
                     (ownerInformationId > 0 && x.InformationId == ownerInformationId) ||
                     subjectLecturerIds.Contains(x.UserId) ||
                     subjectLecturerPersonKeys.Contains(x.InformationId > 0 ? x.InformationId : x.UserId)) &&
                    x.Role.RoleName != "Admin")
                .Select(x => new ManualAssignmentLecturerOptionDto
                {
                    UserId = x.UserId,
                    InformationId = x.InformationId,
                    UserName = x.UserName,
                    FullName = x.Information.LastName + " " + x.Information.FirstName,
                    FacultyId = x.FacultyId,
                    FacultyName = x.Faculty != null ? x.Faculty.FacultyName : string.Empty,
                    RoleName = x.Role.RoleName,
                    IsLecturerRole = x.Role.RoleName == "Giảng viên"
                })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(x => x.PersonKey)
                .Select(g => g
                    .OrderBy(x => GetRolePriority(x.RoleName))
                    .ThenByDescending(x => x.UserId == ownerUserId)
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
                    x.Assignee.IsActive &&
                    personKeySet.Contains((x.NewAssignee != null && x.NewAssignee.InformationId > 0) ? x.NewAssignee.InformationId : (x.Assignee.InformationId > 0 ? x.Assignee.InformationId : x.AssigneeId)) &&
                    x.Status != "Từ chối" &&
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

        public async Task<Dictionary<int, int>> GetSameDayLoadsAsync(
            int semesterId,
            IEnumerable<int> userIds,
            DateTime examDate,
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
                    x.ExamSchedule.ExamDate == examDate &&
                    x.Status != "Từ chối" &&
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

        public async Task<HashSet<int>> GetSubjectLecturerIdsAsync(
            string subjectId,
            CancellationToken cancellationToken = default)
        {
            var rows = await _db.CourseOfferings
                .AsNoTracking()
                .Where(x => x.SubjectId == subjectId && x.User.IsActive && x.User.Role.RoleName == "Giảng viên")
                .Select(x => x.User.InformationId > 0 ? x.User.InformationId : x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return rows.ToHashSet();
        }

        public async Task<List<int>> GetBusyLecturerIdsAsync(
            IEnumerable<int> userIds,
            int slotId,
            DateOnly examDate,
            CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();
            var candidatePersonKeys = await GetPersonKeysByUserIdAsync(ids, cancellationToken);
            var personKeySet = candidatePersonKeys.Values.ToHashSet();

            var busyPersonKeys = await _db.LecturerBusySlots
                .AsNoTracking()
                .Where(x =>
                    personKeySet.Contains(x.User.InformationId > 0 ? x.User.InformationId : x.UserId) &&
                    x.SlotId == slotId &&
                    x.BusyDate == examDate)
                .Select(x => x.User.InformationId > 0 ? x.User.InformationId : x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var busySet = busyPersonKeys.ToHashSet();
            return candidatePersonKeys
                .Where(x => busySet.Contains(x.Value))
                .Select(x => x.Key)
                .ToList();
        }

        public async Task<List<int>> GetConflictingLecturerIdsAsync(
            int scheduleId,
            int slotId,
            DateTime examDate,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();
            var candidatePersonKeys = await GetPersonKeysByUserIdAsync(ids, cancellationToken);
            var personKeySet = candidatePersonKeys.Values.ToHashSet();

            var matchingAssignments = _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.ExamScheduleId != scheduleId &&
                    x.ExamSchedule.SlotId == slotId &&
                    x.ExamSchedule.ExamDate == examDate &&
                    x.Status != "Từ chối" &&
                    (x.InvigilatorResponses
                        .Where(r => r.UserId == (x.NewAssigneeId ?? x.AssigneeId))
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault() ?? string.Empty) != "Từ chối" &&
                    personKeySet.Contains(x.Assignee.InformationId > 0 ? x.Assignee.InformationId : x.AssigneeId))
                .Select(x => x.Assignee.InformationId > 0 ? x.Assignee.InformationId : x.AssigneeId);

            var matchingReplacements = _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.ExamScheduleId != scheduleId &&
                    x.ExamSchedule.SlotId == slotId &&
                    x.ExamSchedule.ExamDate == examDate &&
                    x.Status != "Từ chối" &&
                    (x.InvigilatorResponses
                        .Where(r => r.UserId == (x.NewAssigneeId ?? x.AssigneeId))
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault() ?? string.Empty) != "Từ chối" &&
                    x.NewAssigneeId.HasValue &&
                    personKeySet.Contains(x.NewAssignee!.InformationId > 0 ? x.NewAssignee.InformationId : x.NewAssigneeId.Value))
                .Select(x => x.NewAssignee!.InformationId > 0 ? x.NewAssignee.InformationId : x.NewAssigneeId!.Value);

            var conflictingPersonKeys = await matchingAssignments.Concat(matchingReplacements).Distinct().ToListAsync(cancellationToken);
            var conflictSet = conflictingPersonKeys.ToHashSet();
            return candidatePersonKeys
                .Where(x => conflictSet.Contains(x.Value))
                .Select(x => x.Key)
                .ToList();
        }

        public async Task SaveAsync(
            ManualAssignmentSavePlanDto plan,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var schedule = await _db.ExamSchedules
                    .FirstOrDefaultAsync(x => x.ExamScheduleId == plan.ExamScheduleId, cancellationToken);

                if (schedule is null)
                    throw new InvalidOperationException("Không tìm thấy lịch thi.");

                var existing = await _db.ExamInvigilators
                    .Where(x => x.ExamScheduleId == plan.ExamScheduleId)
                    .ToListAsync(cancellationToken);

                var selectedUserIds = plan.NewInvigilators.Select(x => x.AssigneeId)
                    .Concat(plan.ReplaceInvigilators.Select(x => x.NewAssigneeId))
                    .Distinct()
                    .ToList();
                var selectedPersonKeys = await GetPersonKeysByUserIdAsync(selectedUserIds, cancellationToken);
                var existingPersonKeys = await _db.ExamInvigilators
                    .AsNoTracking()
                    .Where(x => x.ExamScheduleId == plan.ExamScheduleId)
                    .Select(x => (x.NewAssignee != null && x.NewAssignee.InformationId > 0) ? x.NewAssignee.InformationId : (x.Assignee.InformationId > 0 ? x.Assignee.InformationId : x.AssigneeId))
                    .ToListAsync(cancellationToken);
                var existingPersonKeySet = existingPersonKeys.ToHashSet();

                if (existing.Count >= 2 && plan.NewInvigilators.Any())
                    throw new InvalidOperationException("Lịch thi này đã đủ 2 giám thị, không thể phân công thêm.");

                foreach (var item in plan.NewInvigilators)
                {
                    if (selectedPersonKeys.TryGetValue(item.AssigneeId, out var personKey) && existingPersonKeySet.Contains(personKey))
                        throw new InvalidOperationException("Một giảng viên đã được phân công cho lịch thi này.");

                    _db.ExamInvigilators.Add(new Data.Entities.ExamInvigilator
                    {
                        AssigneeId = item.AssigneeId,
                        AssignerId = item.AssignerId,
                        ExamScheduleId = item.ExamScheduleId,
                        PositionNo = item.PositionNo,
                        Status = item.Status,
                        CreateAt = item.CreateAt,
                        UpdateAt = item.UpdateAt
                    });
                    if (personKey > 0)
                        existingPersonKeySet.Add(personKey);
                }

                foreach (var item in plan.ReplaceInvigilators)
                {
                    var existingItem = existing.FirstOrDefault(x => x.ExamInvigilatorId == item.ExamInvigilatorId);
                    if (existingItem is null)
                        throw new InvalidOperationException("Không tìm thấy vị trí giám thị cần thay thế.");

                    if (selectedPersonKeys.TryGetValue(item.NewAssigneeId, out var personKey) && existingPersonKeySet.Contains(personKey))
                        throw new InvalidOperationException("Một giảng viên đã được phân công cho lịch thi này.");

                    existingItem.NewAssigneeId = item.NewAssigneeId;
                    existingItem.AssignerId = item.AssignerId;
                    existingItem.Status = ExamInvigilatorStatuses.PendingConfirmation;
                    existingItem.ConfirmationSentAt = null;
                    existingItem.UpdateAt = item.UpdateAt;
                    if (personKey > 0)
                        existingPersonKeySet.Add(personKey);
                }

                schedule.Status = plan.StatusAfter;

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
