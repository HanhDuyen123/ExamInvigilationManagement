using ExamInvigilationManagement.Application.DTOs.InvigilatorResponse;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Constants;
using ExamInvigilationManagement.Common.Workflow;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Data.Entities;
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

            var userInformationId = await GetUserInformationIdAsync(userId, cancellationToken);

            var query = _db.ExamInvigilators
                .AsNoTracking()
                .Where(x => x.Status != ExamInvigilatorStatuses.Cancelled)
                .Where(x => (x.NewAssigneeId ?? x.AssigneeId) == userId ||
                    (userInformationId.HasValue && ((x.NewAssignee != null ? x.NewAssignee.InformationId : x.Assignee.InformationId) == userInformationId.Value)))
                .Where(x =>
                    x.ExamSchedule.Status == ExamScheduleStatuses.Approved ||
                    x.InvigilatorResponses.Any(r =>
                        (r.UserId == userId || (userInformationId.HasValue && r.User.InformationId == userInformationId.Value)) &&
                        r.Status == InvigilatorResponseStatuses.Rejected))
                .Select(x => new
                {
                    Invigilator = x,
                    Schedule = x.ExamSchedule,
                    LatestResponse = x.InvigilatorResponses
                        .Where(r => r.UserId == userId || (userInformationId.HasValue && r.User.InformationId == userInformationId.Value))
                        .OrderByDescending(r => r.ResponseAt)
                        .FirstOrDefault(),
                    LatestSubstitution = x.InvigilatorSubstitutions
                        .Where(s => s.UserId == userId || (userInformationId.HasValue && s.User.InformationId == userInformationId.Value))
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
                    (x.Schedule.ExamFormat != null ? (x.Schedule.ExamFormat.Code + " " + x.Schedule.ExamFormat.Name) : "").ToLower().Contains(keyword) ||
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
                ExamFormatDisplay = x.Schedule.ExamFormat != null ? x.Schedule.ExamFormat.Code + " - " + x.Schedule.ExamFormat.Name : string.Empty,
                BuildingId = x.Schedule.Room.BuildingId,
                RoomName = x.Schedule.Room.RoomName,
                AcademyYearName = x.Schedule.AcademyYear.AcademyYearName,
                SemesterName = x.Schedule.Semester.SemesterName,
                PeriodName = x.Schedule.Period.PeriodName,
                SessionName = x.Schedule.Session.SessionName,
                SlotName = x.Schedule.Slot.SlotName,
                TimeStart = x.Schedule.Slot.TimeStart,
                ExamDate = x.Schedule.ExamDate,
                Lecturer1Name = x.Schedule.ExamInvigilators.Where(i => i.PositionNo == 1 && i.Status != ExamInvigilatorStatuses.Cancelled).Select(i => i.NewAssignee != null ? i.NewAssignee.Information.LastName + " " + i.NewAssignee.Information.FirstName : i.Assignee.Information.LastName + " " + i.Assignee.Information.FirstName).FirstOrDefault(),
                Lecturer2Name = x.Schedule.ExamInvigilators.Where(i => i.PositionNo == 2 && i.Status != ExamInvigilatorStatuses.Cancelled).Select(i => i.NewAssignee != null ? i.NewAssignee.Information.LastName + " " + i.NewAssignee.Information.FirstName : i.Assignee.Information.LastName + " " + i.Assignee.Information.FirstName).FirstOrDefault(),
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

        public async Task<DateTime?> GetFirstAssignmentDateAsync(
            int userId,
            InvigilatorAssignmentSearchDto search,
            CancellationToken cancellationToken = default)
        {
            var userInformationId = await GetUserInformationIdAsync(userId, cancellationToken);
            var query = BuildAssignmentBaseQuery(userId, userInformationId, search);

            return await query
                .OrderBy(x => x.Schedule.ExamDate)
                .Select(x => (DateTime?)x.Schedule.ExamDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<InvigilatorAssignmentItemDto>> GetAssignmentsForCalendarAsync(
            int userId,
            InvigilatorAssignmentSearchDto search,
            DateTime weekStart,
            DateTime weekEnd,
            CancellationToken cancellationToken = default)
        {
            var userInformationId = await GetUserInformationIdAsync(userId, cancellationToken);
            var query = BuildAssignmentBaseQuery(userId, userInformationId, search)
                .Where(x => x.Schedule.ExamDate >= weekStart.Date && x.Schedule.ExamDate <= weekEnd.Date);

            return await ProjectAssignments(query)
                .OrderBy(x => x.ExamDate)
                .ThenBy(x => x.TimeStart)
                .ThenBy(x => x.SubjectId)
                .ToListAsync(cancellationToken);
        }

        private IQueryable<AssignmentQueryRow> BuildAssignmentBaseQuery(
            int userId,
            int? userInformationId,
            InvigilatorAssignmentSearchDto search)
        {
            var query = _db.ExamInvigilators
                .AsNoTracking()
                .Where(x => x.Status != ExamInvigilatorStatuses.Cancelled)
                .Where(x => (x.NewAssigneeId ?? x.AssigneeId) == userId ||
                    (userInformationId.HasValue && ((x.NewAssignee != null ? x.NewAssignee.InformationId : x.Assignee.InformationId) == userInformationId.Value)))
                .Where(x =>
                    x.ExamSchedule.Status == ExamScheduleStatuses.Approved ||
                    x.InvigilatorResponses.Any(r =>
                        (r.UserId == userId || (userInformationId.HasValue && r.User.InformationId == userInformationId.Value)) &&
                        r.Status == InvigilatorResponseStatuses.Rejected))
                .Select(x => new AssignmentQueryRow
                {
                    Invigilator = x,
                    Schedule = x.ExamSchedule,
                    LatestResponse = x.InvigilatorResponses
                        .Where(r => r.UserId == userId || (userInformationId.HasValue && r.User.InformationId == userInformationId.Value))
                        .OrderByDescending(r => r.ResponseAt)
                        .FirstOrDefault(),
                    LatestSubstitution = x.InvigilatorSubstitutions
                        .Where(s => s.UserId == userId || (userInformationId.HasValue && s.User.InformationId == userInformationId.Value))
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
                    (x.Schedule.ExamFormat != null ? (x.Schedule.ExamFormat.Code + " " + x.Schedule.ExamFormat.Name) : "").ToLower().Contains(keyword) ||
                    (x.Schedule.Offering.ClassName ?? "").ToLower().Contains(keyword));
            }

            return query;
        }

        private static IQueryable<InvigilatorAssignmentItemDto> ProjectAssignments(IQueryable<AssignmentQueryRow> query)
        {
            return query.Select(x => new InvigilatorAssignmentItemDto
            {
                ExamInvigilatorId = x.Invigilator.ExamInvigilatorId,
                ExamScheduleId = x.Schedule.ExamScheduleId,
                PositionNo = x.Invigilator.PositionNo,
                SubjectId = x.Schedule.Offering.SubjectId,
                SubjectName = x.Schedule.Offering.Subject.SubjectName,
                ClassName = x.Schedule.Offering.ClassName,
                GroupNumber = x.Schedule.Offering.GroupNumber,
                ExamFormatDisplay = x.Schedule.ExamFormat != null ? x.Schedule.ExamFormat.Code + " - " + x.Schedule.ExamFormat.Name : string.Empty,
                BuildingId = x.Schedule.Room.BuildingId,
                RoomName = x.Schedule.Room.RoomName,
                AcademyYearName = x.Schedule.AcademyYear.AcademyYearName,
                SemesterName = x.Schedule.Semester.SemesterName,
                PeriodName = x.Schedule.Period.PeriodName,
                SessionName = x.Schedule.Session.SessionName,
                SlotName = x.Schedule.Slot.SlotName,
                TimeStart = x.Schedule.Slot.TimeStart,
                ExamDate = x.Schedule.ExamDate,
                Lecturer1Name = x.Schedule.ExamInvigilators.Where(i => i.PositionNo == 1 && i.Status != ExamInvigilatorStatuses.Cancelled).Select(i => i.NewAssignee != null ? i.NewAssignee.Information.LastName + " " + i.NewAssignee.Information.FirstName : i.Assignee.Information.LastName + " " + i.Assignee.Information.FirstName).FirstOrDefault(),
                Lecturer2Name = x.Schedule.ExamInvigilators.Where(i => i.PositionNo == 2 && i.Status != ExamInvigilatorStatuses.Cancelled).Select(i => i.NewAssignee != null ? i.NewAssignee.Information.LastName + " " + i.NewAssignee.Information.FirstName : i.Assignee.Information.LastName + " " + i.Assignee.Information.FirstName).FirstOrDefault(),
                ResponseStatus = x.LatestResponse == null ? "Chưa phản hồi" : x.LatestResponse.Status,
                ResponseNote = x.LatestResponse == null ? null : x.LatestResponse.Note,
                HasSubstitutionProposal = x.LatestSubstitution != null,
                SubstitutionStatus = x.LatestSubstitution == null ? string.Empty : x.LatestSubstitution.Status
            });
        }

        public async Task MarkConfirmationSentAsync(IEnumerable<int> scheduleIds, int lecturerUserId, CancellationToken cancellationToken = default)
        {
            var ids = scheduleIds.Distinct().ToList();
            var invigilators = await _db.ExamInvigilators
                .Where(x =>
                    ids.Contains(x.ExamScheduleId) &&
                    (x.NewAssigneeId ?? x.AssigneeId) == lecturerUserId)
                .ToListAsync(cancellationToken);

            var sentAt = DateTime.Now;

            foreach (var item in invigilators)
            {
                item.Status = "Chờ xác nhận";
                item.UpdateAt = sentAt;
                item.ConfirmationSentAt = sentAt;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> AutoConfirmExpiredAsync(TimeSpan responseWindow, CancellationToken cancellationToken = default)
        {
            var deadline = DateTime.Now.Subtract(responseWindow);
            var targets = await _db.ExamInvigilators
                .Where(x => x.Status == ExamInvigilatorStatuses.PendingConfirmation && x.ConfirmationSentAt.HasValue && x.ConfirmationSentAt.Value <= deadline && !x.InvigilatorResponses.Any(r => r.UserId == (x.NewAssigneeId ?? x.AssigneeId)))
                .ToListAsync(cancellationToken);

            foreach (var target in targets)
            {
                    var assigneeId = target.NewAssigneeId ?? target.AssigneeId;
                    target.Status = InvigilatorResponseStatuses.Confirmed;
                    target.UpdateAt = DateTime.Now;
                    _db.InvigilatorResponses.Add(new Data.Entities.InvigilatorResponse
                    {
                        ExamInvigilatorId = target.ExamInvigilatorId,
                        UserId = assigneeId,
                        Status = InvigilatorResponseStatuses.Confirmed,
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
                    AssigneeId = x.NewAssigneeId ?? x.AssigneeId,
                    AssigneeInformationId = x.NewAssignee != null ? x.NewAssignee.InformationId : x.Assignee.InformationId,
                    FacultyId = x.ExamSchedule.Offering.Subject.FacultyId,
                    ScheduleStatus = x.ExamSchedule.Status,
                    ConfirmationSentAt = x.ConfirmationSentAt,
                    SubjectId = x.ExamSchedule.Offering.SubjectId
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<int?> GetUserInformationIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (int?)x.InformationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpsertResponsesAsync(
            int userId,
            IEnumerable<int> examInvigilatorIds,
            string status,
            string? note,
            CancellationToken cancellationToken = default)
        {
            var ids = examInvigilatorIds.Distinct().ToList();
            var userInformationId = await GetUserInformationIdAsync(userId, cancellationToken);
            var existing = await _db.InvigilatorResponses
                .Where(x => ids.Contains(x.ExamInvigilatorId) && (x.UserId == userId || (userInformationId.HasValue && x.User.InformationId == userInformationId.Value)))
                .ToListAsync(cancellationToken);

            foreach (var id in ids)
            {
                var response = existing.FirstOrDefault(x => x.ExamInvigilatorId == id);
                if (response == null)
                {
                    InvigilatorWorkflowGuard.EnsureResponseStatusChange(null, status, $"Phản hồi coi thi #{id}");
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
                    InvigilatorWorkflowGuard.EnsureResponseStatusChange(response.Status, status, $"Phản hồi coi thi #{id}");
                    response.Status = status;
                    response.Note = note;
                    response.ResponseAt = DateTime.Now;
                }
            }

            var invigilators = await _db.ExamInvigilators
                .Include(x => x.ExamSchedule)
                .Where(x => ids.Contains(x.ExamInvigilatorId) && ((x.NewAssigneeId ?? x.AssigneeId) == userId ||
                    (userInformationId.HasValue && ((x.NewAssignee != null ? x.NewAssignee.InformationId : x.Assignee.InformationId) == userInformationId.Value))))
                .ToListAsync(cancellationToken);

            foreach (var invigilator in invigilators)
            {
                invigilator.Status = status;
                invigilator.UpdateAt = DateTime.Now;
                if (string.Equals(status, InvigilatorResponseStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
                    invigilator.ExamSchedule.Status = ExamScheduleStatuses.MissingInvigilator;
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
                    ExamFormatDisplay = x.ExamFormat != null ? x.ExamFormat.Code + " - " + x.ExamFormat.Name : string.Empty,
                    BuildingId = x.Room.BuildingId,
                    RoomName = x.Room.RoomName,
                    ExamDate = x.ExamDate,
                    SlotName = x.Slot.SlotName,
                    TimeStart = x.Slot.TimeStart,
                    Lecturers = x.ExamInvigilators
                    .Where(i => i.Status != InvigilatorResponseStatuses.Rejected && i.Status != ExamInvigilatorStatuses.Cancelled)
                    .Select(i => new InvigilatorConfirmationLecturerDto
                    {
                        UserId = i.NewAssignee != null ? i.NewAssignee.UserId : i.Assignee.UserId,
                        UserName = i.NewAssignee != null ? i.NewAssignee.UserName : i.Assignee.UserName,
                        FullName = i.NewAssignee != null
                            ? (i.NewAssignee.Information == null ? i.NewAssignee.UserName : i.NewAssignee.Information.LastName + " " + i.NewAssignee.Information.FirstName)
                            : (i.Assignee.Information == null ? i.Assignee.UserName : i.Assignee.Information.LastName + " " + i.Assignee.Information.FirstName),
                        Email = i.NewAssignee != null
                            ? (i.NewAssignee.Information == null ? null : i.NewAssignee.Information.Email)
                            : (i.Assignee.Information == null ? null : i.Assignee.Information.Email)
                    }).ToList()
                })
                .ToListAsync(cancellationToken);
        }

        private sealed class AssignmentQueryRow
        {
            public ExamInvigilator Invigilator { get; set; } = null!;
            public ExamSchedule Schedule { get; set; } = null!;
            public Data.Entities.InvigilatorResponse? LatestResponse { get; set; }
            public InvigilatorSubstitution? LatestSubstitution { get; set; }
        }
    }
}
