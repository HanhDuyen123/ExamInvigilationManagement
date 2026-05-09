using ExamInvigilationManagement.Application.DTOs.ManualAssignment;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
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
                .Where(x => x.ExamScheduleId == scheduleId && x.Offering.User.FacultyId == facultyId)
                .Select(x => new ManualAssignmentScheduleDto
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
                    OfferingUserName = x.Offering.User.UserName,
                    OfferingUserFullName = x.Offering.User.Information.LastName + " " + x.Offering.User.Information.FirstName,
                    OfferingFacultyId = x.Offering.User.FacultyId,
                    SubjectId = x.Offering.SubjectId,
                    SubjectName = x.Offering.Subject.SubjectName,
                    ClassName = x.Offering.ClassName,
                    GroupNumber = x.Offering.GroupNumber,
                    ExamDate = x.ExamDate,
                    Status = x.Status,
                    CurrentInvigilatorCount = x.ExamInvigilators.Count
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
                    UserName = x.Assignee.UserName,
                    FullName = x.Assignee.Information.LastName + " " + x.Assignee.Information.FirstName,
                    NewUserId = x.NewAssigneeId,
                    NewUserName = x.NewAssignee != null ? x.NewAssignee.UserName : string.Empty,
                    NewFullName = x.NewAssignee != null ? x.NewAssignee.Information.LastName + " " + x.NewAssignee.Information.FirstName : string.Empty,
                    PositionNo = x.PositionNo,
                    Status = x.Status,
                    ResponseStatus = x.InvigilatorResponses
                        .Where(r => r.UserId == x.AssigneeId)
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault() ?? (x.Status == "Chờ xác nhận" ? "Chờ phản hồi" : x.Status),
                    ResponseNote = x.InvigilatorResponses
                        .Where(r => r.UserId == x.AssigneeId)
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Note)
                        .FirstOrDefault() ?? string.Empty,
                    ResponseAt = x.InvigilatorResponses
                        .Where(r => r.UserId == x.AssigneeId)
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
            CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.FacultyId == facultyId &&
                    x.Role.RoleName == "Giảng viên")
                .Select(x => new ManualAssignmentLecturerOptionDto
                {
                    UserId = x.UserId,
                    UserName = x.UserName,
                    FullName = x.Information.LastName + " " + x.Information.FirstName,
                    FacultyId = x.FacultyId,
                    FacultyName = x.Faculty != null ? x.Faculty.FacultyName : string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetLecturerLoadsAsync(
            int semesterId,
            int facultyId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.ExamSchedule.SemesterId == semesterId &&
                    x.Assignee.FacultyId == facultyId &&
                    x.Assignee.IsActive)
                .GroupBy(x => x.AssigneeId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetSameDayLoadsAsync(
            int semesterId,
            int facultyId,
            DateTime examDate,
            CancellationToken cancellationToken = default)
        {
            return await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.ExamSchedule.SemesterId == semesterId &&
                    x.Assignee.FacultyId == facultyId &&
                    x.ExamSchedule.ExamDate == examDate)
                .GroupBy(x => x.AssigneeId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
        }

        public async Task<List<int>> GetBusyLecturerIdsAsync(
            IEnumerable<int> userIds,
            int slotId,
            DateOnly examDate,
            CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();

            return await _db.LecturerBusySlots
                .AsNoTracking()
                .Where(x =>
                    ids.Contains(x.UserId) &&
                    x.SlotId == slotId &&
                    x.BusyDate == examDate)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task<List<int>> GetConflictingLecturerIdsAsync(
            int scheduleId,
            int slotId,
            DateTime examDate,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();

            var matchingAssignments = _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.ExamScheduleId != scheduleId &&
                    x.ExamSchedule.SlotId == slotId &&
                    x.ExamSchedule.ExamDate == examDate &&
                    ids.Contains(x.AssigneeId))
                .Select(x => x.AssigneeId);

            var matchingReplacements = _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.ExamScheduleId != scheduleId &&
                    x.ExamSchedule.SlotId == slotId &&
                    x.ExamSchedule.ExamDate == examDate &&
                    x.NewAssigneeId.HasValue &&
                    ids.Contains(x.NewAssigneeId.Value))
                .Select(x => x.NewAssigneeId!.Value);

            return await matchingAssignments.Concat(matchingReplacements).Distinct().ToListAsync(cancellationToken);
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

                var existingUserIds = existing.Select(x => x.AssigneeId).ToHashSet();

                if (existing.Count >= 2)
                    throw new InvalidOperationException("Lịch thi này đã đủ 2 giám thị, không thể phân công thêm.");

                foreach (var item in plan.NewInvigilators)
                {
                    if (existingUserIds.Contains(item.AssigneeId))
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
    }
}
