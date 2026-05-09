using ExamInvigilationManagement.Application.DTOs.InvigilatorResponse;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class InvigilatorResponseRepository : IInvigilatorResponseRepository
    {
        private readonly ApplicationDbContext _db;

        public InvigilatorResponseRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<InvigilatorAssignmentItemDto>> GetAssignmentsAsync(
            int userId,
            InvigilatorAssignmentSearchDto search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var query = _db.ExamInvigilators
                .AsNoTracking()
                .Where(x => x.AssigneeId == userId)
                .Select(x => new
                {
                    Invigilator = x,
                    Schedule = x.ExamSchedule,
                    LatestResponse = x.InvigilatorResponses
                        .Where(r => r.UserId == userId)
                        .OrderByDescending(r => r.ResponseAt)
                        .FirstOrDefault(),
                    LatestSubstitution = x.InvigilatorSubstitutions
                        .Where(s => s.UserId == userId)
                        .OrderByDescending(s => s.CreateAt)
                        .FirstOrDefault()
                });

            if (!string.IsNullOrWhiteSpace(search.SubjectId))
                query = query.Where(x => x.Schedule.Offering.SubjectId == search.SubjectId);

            if (!string.IsNullOrWhiteSpace(search.BuildingId))
                query = query.Where(x => x.Schedule.Room.BuildingId == search.BuildingId);

            if (search.RoomId.HasValue)
                query = query.Where(x => x.Schedule.RoomId == search.RoomId.Value);

            if (search.AcademyYearId.HasValue)
                query = query.Where(x => x.Schedule.AcademyYearId == search.AcademyYearId.Value);

            if (search.SemesterId.HasValue)
                query = query.Where(x => x.Schedule.SemesterId == search.SemesterId.Value);

            if (search.PeriodId.HasValue)
                query = query.Where(x => x.Schedule.PeriodId == search.PeriodId.Value);

            if (search.SessionId.HasValue)
                query = query.Where(x => x.Schedule.SessionId == search.SessionId.Value);

            if (search.SlotId.HasValue)
                query = query.Where(x => x.Schedule.SlotId == search.SlotId.Value);

            if (search.FromDate.HasValue)
                query = query.Where(x => x.Schedule.ExamDate >= search.FromDate.Value.Date);

            if (search.ToDate.HasValue)
                query = query.Where(x => x.Schedule.ExamDate <= search.ToDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(search.Status))
                query = search.Status == "Chưa phản hồi"
                    ? query.Where(x => x.LatestResponse == null)
                    : query.Where(x => x.LatestResponse != null && x.LatestResponse.Status == search.Status);

            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var keyword = search.Keyword.Trim().ToLower();
                query = query.Where(x =>
                    (x.Schedule.Offering.SubjectId ?? "").ToLower().Contains(keyword) ||
                    (x.Schedule.Offering.Subject.SubjectName ?? "").ToLower().Contains(keyword) ||
                    (x.Schedule.Offering.ClassName ?? "").ToLower().Contains(keyword));
            }

            var projected = query.Select(x => new InvigilatorAssignmentItemDto
            {
                ExamInvigilatorId = x.Invigilator.ExamInvigilatorId,
                ExamScheduleId = x.Schedule.ExamScheduleId,
                PositionNo = x.Invigilator.PositionNo,
                SubjectId = x.Schedule.Offering.SubjectId,
                SubjectName = x.Schedule.Offering.Subject.SubjectName,
                ClassName = x.Schedule.Offering.ClassName,
                GroupNumber = x.Schedule.Offering.GroupNumber,
                BuildingId = x.Schedule.Room.BuildingId,
                RoomName = x.Schedule.Room.RoomName,
                AcademyYearName = x.Schedule.AcademyYear.AcademyYearName,
                SemesterName = x.Schedule.Semester.SemesterName,
                PeriodName = x.Schedule.Period.PeriodName,
                SessionName = x.Schedule.Session.SessionName,
                SlotName = x.Schedule.Slot.SlotName,
                TimeStart = x.Schedule.Slot.TimeStart,
                ExamDate = x.Schedule.ExamDate,
                Lecturer1Name = x.Schedule.ExamInvigilators.Where(i => i.PositionNo == 1).Select(i => i.Assignee.Information.LastName + " " + i.Assignee.Information.FirstName).FirstOrDefault(),
                Lecturer2Name = x.Schedule.ExamInvigilators.Where(i => i.PositionNo == 2).Select(i => i.Assignee.Information.LastName + " " + i.Assignee.Information.FirstName).FirstOrDefault(),
                ResponseStatus = x.LatestResponse == null ? "Chưa phản hồi" : x.LatestResponse.Status,
                ResponseNote = x.LatestResponse == null ? null : x.LatestResponse.Note,
                HasSubstitutionProposal = x.LatestSubstitution != null,
                SubstitutionStatus = x.LatestSubstitution == null ? string.Empty : x.LatestSubstitution.Status
            });

            var total = await projected.CountAsync(cancellationToken);
            var items = await projected
                .OrderBy(x => x.ExamDate)
                .ThenBy(x => x.TimeStart)
                .ThenBy(x => x.SubjectId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<InvigilatorAssignmentItemDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task MarkConfirmationSentAsync(IEnumerable<int> scheduleIds, CancellationToken cancellationToken = default)
        {
            var ids = scheduleIds.Distinct().ToList();
            var invigilators = await _db.ExamInvigilators
                .Where(x => ids.Contains(x.ExamScheduleId))
                .ToListAsync(cancellationToken);

            foreach (var item in invigilators)
            {
                item.Status = "Chờ xác nhận";
                item.UpdateAt = DateTime.Now;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> AutoConfirmExpiredAsync(TimeSpan responseWindow, CancellationToken cancellationToken = default)
        {
            var deadline = DateTime.Now.Subtract(responseWindow);
            var targets = await _db.ExamInvigilators
                .Where(x => x.Status == "Chờ xác nhận" && x.UpdateAt.HasValue && x.UpdateAt.Value <= deadline && !x.InvigilatorResponses.Any(r => r.UserId == x.AssigneeId))
                .ToListAsync(cancellationToken);

            foreach (var target in targets)
            {
                target.Status = "Xác nhận";
                target.UpdateAt = DateTime.Now;
                _db.InvigilatorResponses.Add(new Data.Entities.InvigilatorResponse
                {
                    ExamInvigilatorId = target.ExamInvigilatorId,
                    UserId = target.AssigneeId,
                    Status = "Xác nhận",
                    Note = "Hệ thống tự động xác nhận sau 48 giờ kể từ khi gửi yêu cầu xác nhận.",
                    ResponseAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            return targets.Count;
        }

        public async Task<List<InvigilatorAssignmentSubmitItemDto>> GetSubmitItemsAsync(
            IEnumerable<int> examInvigilatorIds,
            CancellationToken cancellationToken = default)
        {
            var ids = examInvigilatorIds.Distinct().ToList();
            return await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x => ids.Contains(x.ExamInvigilatorId))
                .Select(x => new InvigilatorAssignmentSubmitItemDto
                {
                    ExamInvigilatorId = x.ExamInvigilatorId,
                    ExamScheduleId = x.ExamScheduleId,
                    AssigneeId = x.AssigneeId,
                    FacultyId = x.ExamSchedule.Offering.Subject.FacultyId,
                    ScheduleStatus = x.ExamSchedule.Status,
                    SubjectId = x.ExamSchedule.Offering.SubjectId
                })
                .ToListAsync(cancellationToken);
        }

        public async Task UpsertResponsesAsync(
            int userId,
            IEnumerable<int> examInvigilatorIds,
            string status,
            string? note,
            CancellationToken cancellationToken = default)
        {
            var ids = examInvigilatorIds.Distinct().ToList();
            var existing = await _db.InvigilatorResponses
                .Where(x => ids.Contains(x.ExamInvigilatorId) && x.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (var id in ids)
            {
                var response = existing.FirstOrDefault(x => x.ExamInvigilatorId == id);
                if (response == null)
                {
                    _db.InvigilatorResponses.Add(new Data.Entities.InvigilatorResponse
                    {
                        ExamInvigilatorId = id,
                        UserId = userId,
                        Status = status,
                        Note = note,
                        ResponseAt = DateTime.Now
                    });
                }
                else
                {
                    response.Status = status;
                    response.Note = note;
                    response.ResponseAt = DateTime.Now;
                }
            }

            var invigilators = await _db.ExamInvigilators
                .Where(x => ids.Contains(x.ExamInvigilatorId) && x.AssigneeId == userId)
                .ToListAsync(cancellationToken);

            foreach (var invigilator in invigilators)
            {
                invigilator.Status = status;
                invigilator.UpdateAt = DateTime.Now;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<InvigilatorNotificationUserDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new InvigilatorNotificationUserDto
                {
                    UserId = x.UserId,
                    UserName = x.UserName,
                    FullName = x.Information == null ? x.UserName : x.Information.LastName + " " + x.Information.FirstName,
                    Email = x.Information == null ? null : x.Information.Email
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<InvigilatorNotificationUserDto>> GetActiveSecretariesAsync(IEnumerable<int> facultyIds, CancellationToken cancellationToken = default)
        {
            var ids = facultyIds.Distinct().ToList();
            return await _db.Users
                .AsNoTracking()
                .Where(x => x.IsActive && x.FacultyId.HasValue && ids.Contains(x.FacultyId.Value) && x.Role.RoleName == "Thư ký khoa")
                .Select(x => new InvigilatorNotificationUserDto
                {
                    UserId = x.UserId,
                    UserName = x.UserName,
                    FullName = x.Information == null ? x.UserName : x.Information.LastName + " " + x.Information.FirstName,
                    Email = x.Information == null ? null : x.Information.Email
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<InvigilatorConfirmationScheduleDto>> GetConfirmationSchedulesAsync(IEnumerable<int> scheduleIds, CancellationToken cancellationToken = default)
        {
            var ids = scheduleIds.Distinct().ToList();
            return await _db.ExamSchedules
                .AsNoTracking()
                .Where(x => ids.Contains(x.ExamScheduleId))
                .Select(x => new InvigilatorConfirmationScheduleDto
                {
                    ExamScheduleId = x.ExamScheduleId,
                    FacultyId = x.Offering.Subject.FacultyId,
                    Status = x.Status,
                    SubjectId = x.Offering.SubjectId,
                    SubjectName = x.Offering.Subject.SubjectName,
                    ClassName = x.Offering.ClassName,
                    GroupNumber = x.Offering.GroupNumber,
                    BuildingId = x.Room.BuildingId,
                    RoomName = x.Room.RoomName,
                    ExamDate = x.ExamDate,
                    SlotName = x.Slot.SlotName,
                    TimeStart = x.Slot.TimeStart,
                    Lecturers = x.ExamInvigilators.Select(i => new InvigilatorConfirmationLecturerDto
                    {
                        UserId = i.Assignee.UserId,
                        UserName = i.Assignee.UserName,
                        FullName = i.Assignee.Information == null ? i.Assignee.UserName : i.Assignee.Information.LastName + " " + i.Assignee.Information.FirstName,
                        Email = i.Assignee.Information == null ? null : i.Assignee.Information.Email
                    }).ToList()
                })
                .ToListAsync(cancellationToken);
        }
    }
}
