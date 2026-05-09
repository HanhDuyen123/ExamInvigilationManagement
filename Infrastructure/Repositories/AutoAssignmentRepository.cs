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
                    x.Offering.User.FacultyId == facultyId)
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
                    OfferingFacultyId = x.Offering.User.FacultyId,

                    SubjectId = x.Offering.SubjectId,
                    SubjectName = x.Offering.Subject.SubjectName,
                    ClassName = x.Offering.ClassName,
                    GroupNumber = x.Offering.GroupNumber,

                    ExamDate = x.ExamDate,
                    Status = x.Status
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AutoAssignLecturerDto>> GetActiveLecturersAsync(
            int facultyId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.FacultyId == facultyId &&
                    x.Role.RoleName == "Giảng viên")
                .Select(x => new AutoAssignLecturerDto
                {
                    UserId = x.UserId,
                    UserName = x.UserName,
                    FullName = x.Information.LastName + " " + x.Information.FirstName,
                    FacultyId = x.FacultyId,
                    FacultyName = x.Faculty != null ? x.Faculty.FacultyName : string.Empty,
                    IsActive = x.IsActive
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
                    ExamScheduleId = x.ExamScheduleId,
                    UserId = x.AssigneeId,
                    SlotId = x.ExamSchedule.SlotId,
                    ExamDate = x.ExamSchedule.ExamDate
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