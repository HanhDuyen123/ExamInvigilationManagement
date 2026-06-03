using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Constants;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class ExamScheduleRepository : IExamScheduleRepository
    {
        private readonly ApplicationDbContext _context;

        public ExamScheduleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ExamScheduleDto>> GetPagedAsync(ExamScheduleSearchDto filter, int page, int pageSize)
        {
            var query = _context.ExamSchedules
                .AsNoTracking()
                .Include(x => x.AcademyYear)
                .Include(x => x.Semester)
                .ThenInclude(s => s.AcademyYear)
                .Include(x => x.Period)
                .Include(x => x.Session)
                .Include(x => x.Slot)
                .Include(x => x.Room)
                    .ThenInclude(r => r.Building)
                .Include(x => x.ExamFormat)
                .Include(x => x.Offering)
                    .ThenInclude(o => o.Subject)
                        .ThenInclude(s => s.Faculty)
                .Include(x => x.Offering)
                    .ThenInclude(o => o.User)
                        .ThenInclude(u => u.Information)
                .Include(x => x.Offering)
                    .ThenInclude(o => o.Semester)
                        .ThenInclude(s => s.AcademyYear)
                .Include(x => x.ExamInvigilators)
                    .ThenInclude(i => i.Assignee)
                        .ThenInclude(u => u.Information)
                .Include(x => x.ExamInvigilators)
                    .ThenInclude(i => i.Assignee)
                        .ThenInclude(u => u.Faculty)
                .Include(x => x.ExamInvigilators)
                    .ThenInclude(i => i.NewAssignee)
                        .ThenInclude(u => u.Information)
                .Include(x => x.ExamInvigilators)
                    .ThenInclude(i => i.NewAssignee)
                        .ThenInclude(u => u.Faculty)
                .Include(x => x.ExamScheduleApprovals)
                .AsQueryable();

            if (!string.Equals(filter.CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(filter.CurrentRole))
            {
                query = filter.CurrentFacultyId.HasValue
                    ? query.Where(x => x.Offering.Subject.FacultyId == filter.CurrentFacultyId.Value)
                    : query.Where(x => false);
            }

            if (filter.FacultyId.HasValue)
                query = query.Where(x => x.Offering.Subject.FacultyId == filter.FacultyId.Value);

            if (filter.UserId.HasValue)
                query = query.Where(x => x.Offering.UserId == filter.UserId.Value);

            if (filter.AcademyYearId.HasValue)
                query = query.Where(x => x.AcademyYearId == filter.AcademyYearId.Value);

            if (filter.SemesterId.HasValue)
                query = query.Where(x => x.SemesterId == filter.SemesterId.Value);

            if (filter.PeriodId.HasValue)
                query = query.Where(x => x.PeriodId == filter.PeriodId.Value);

            if (filter.SessionId.HasValue)
                query = query.Where(x => x.SessionId == filter.SessionId.Value);

            if (filter.SlotId.HasValue)
                query = query.Where(x => x.SlotId == filter.SlotId.Value);

            if (!string.IsNullOrWhiteSpace(filter.SubjectId))
                query = query.Where(x => x.Offering.SubjectId == filter.SubjectId);

            if (!string.IsNullOrWhiteSpace(filter.ClassName))
                query = query.Where(x => x.Offering.ClassName.Contains(filter.ClassName));

            if (!string.IsNullOrWhiteSpace(filter.GroupNumber))
                query = query.Where(x => x.Offering.GroupNumber.Contains(filter.GroupNumber));

            if (filter.ExamFormatId.HasValue)
                query = query.Where(x => x.ExamFormatId == filter.ExamFormatId.Value);

            if (!string.IsNullOrWhiteSpace(filter.BuildingId))
                query = query.Where(x => x.Room.BuildingId == filter.BuildingId);

            if (filter.RoomId.HasValue)
                query = query.Where(x => x.RoomId == filter.RoomId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(x => x.Status == filter.Status);

            if (filter.FromDate.HasValue)
            {
                var from = filter.FromDate.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => x.ExamDate >= from);
            }

            if (filter.ToDate.HasValue)
            {
                var to = filter.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(x => x.ExamDate <= to);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();

                query = query.Where(x =>
                    (x.Offering.SubjectId ?? "").ToLower().Contains(kw) ||
                    (x.Offering.Subject.SubjectName ?? "").ToLower().Contains(kw) ||
                    (x.Offering.ClassName ?? "").ToLower().Contains(kw) ||
                    (x.Offering.GroupNumber ?? "").ToLower().Contains(kw) ||
                    (x.Offering.User.UserName ?? "").ToLower().Contains(kw) ||
                    (x.ExamFormat != null ? (x.ExamFormat.Code + " " + x.ExamFormat.Name) : "").ToLower().Contains(kw) ||
                    ((x.Offering.User.Information != null
                        ? (x.Offering.User.Information.LastName + " " + x.Offering.User.Information.FirstName)
                        : "")).ToLower().Contains(kw) ||
                    (x.AcademyYear.AcademyYearName ?? "").ToLower().Contains(kw) ||
                    (x.Semester.SemesterName ?? "").ToLower().Contains(kw) ||
                    (x.Period.PeriodName ?? "").ToLower().Contains(kw) ||
                    (x.Session.SessionName ?? "").ToLower().Contains(kw) ||
                    (x.Slot.SlotName ?? "").ToLower().Contains(kw) ||
                    (x.Room.RoomName ?? "").ToLower().Contains(kw) ||
                    (x.Room.Building.BuildingName ?? "").ToLower().Contains(kw) ||
                    (x.Status ?? "").ToLower().Contains(kw)
                );
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.ExamDate)
                .ThenByDescending(x => x.ExamScheduleId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ExamScheduleDto
                {
                    Id = x.ExamScheduleId,
                    OfferingId = x.OfferingId,
                    SlotId = x.SlotId,
                    RoomId = x.RoomId,
                    ExamDate = x.ExamDate,
                    Status = x.Status,
                    ApprovalCount = x.ExamScheduleApprovals.Count(a => a.Status == ExamScheduleStatuses.PendingApproval),
                    InvigilatorCount = x.ExamInvigilators.Count(i =>
                        i.Status != InvigilatorResponseStatuses.Rejected &&
                        (i.InvigilatorResponses
                            .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                            .OrderByDescending(r => r.ResponseAt)
                            .Select(r => r.Status)
                            .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected),
                    IsLockedForScheduleEdit = x.ExamInvigilators.Any(),

                    SubjectId = x.Offering.SubjectId,
                    SubjectName = x.Offering.Subject.SubjectName,
                    Credit = x.Offering.Subject.Credit,
                    FacultyId = x.Offering.Subject.FacultyId,
                    FacultyName = x.Offering.Subject.Faculty != null ? x.Offering.Subject.Faculty.FacultyName : null,
                    ExamFormatId = x.ExamFormatId,
                    ExamFormatCode = x.ExamFormat != null ? x.ExamFormat.Code : null,
                    ExamFormatName = x.ExamFormat != null ? x.ExamFormat.Name : null,
                    UserName = x.Offering.User.Information != null
                        ? $"{x.Offering.User.Information.LastName} {x.Offering.User.Information.FirstName}"
                        : x.Offering.User.UserName,
                    OfferingUserCode = x.Offering.User.UserName,
                    OfferingUserFullName = x.Offering.User.Information != null
                        ? $"{x.Offering.User.Information.LastName} {x.Offering.User.Information.FirstName}"
                        : x.Offering.User.UserName,

                    ClassName = x.Offering.ClassName,
                    GroupNumber = x.Offering.GroupNumber,

                    AcademyYearId = x.AcademyYearId,
                    AcademyYearName = x.AcademyYear.AcademyYearName,

                    SemesterId = x.SemesterId,
                    SemesterName = x.Semester.SemesterName,

                    OfferingAcademyYearId = x.Offering.Semester.AcademyYearId,
                    OfferingSemesterId = x.Offering.SemesterId,

                    PeriodId = x.PeriodId,
                    PeriodName = x.Period.PeriodName,

                    SessionId = x.SessionId,
                    SessionName = x.Session.SessionName,

                    SlotName = $"{x.Slot.SlotName} ({x.Slot.TimeStart:hh\\:mm})",
                    SlotTimeStart = x.Slot.TimeStart,
                    BuildingId = x.Room.BuildingId,
                    BuildingName = x.Room.Building != null ? x.Room.Building.BuildingName : null,
                    RoomName = x.Room.RoomName,
                    RoomCapacity = x.Room.Capacity,

                    Lecturer1Name = x.ExamInvigilators
                        .Where(i =>
                            i.PositionNo == 1 &&
                            i.Status != InvigilatorResponseStatuses.Rejected &&
                            (i.InvigilatorResponses
                                .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                                .OrderByDescending(r => r.ResponseAt)
                                .Select(r => r.Status)
                                .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                        .Select(i => i.NewAssignee != null
                            ? (i.NewAssignee.Information != null ? $"{i.NewAssignee.Information.LastName} {i.NewAssignee.Information.FirstName}" : i.NewAssignee.UserName)
                            : (i.Assignee.Information != null ? $"{i.Assignee.Information.LastName} {i.Assignee.Information.FirstName}" : i.Assignee.UserName))
                        .FirstOrDefault(),

                    Lecturer1Code = x.ExamInvigilators
                        .Where(i =>
                            i.PositionNo == 1 &&
                            i.Status != InvigilatorResponseStatuses.Rejected &&
                            (i.InvigilatorResponses
                                .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                                .OrderByDescending(r => r.ResponseAt)
                                .Select(r => r.Status)
                                .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                        .Select(i => i.NewAssignee != null ? i.NewAssignee.UserName : i.Assignee.UserName)
                        .FirstOrDefault(),

                    Lecturer1FacultyName = x.ExamInvigilators
                        .Where(i =>
                            i.PositionNo == 1 &&
                            i.Status != InvigilatorResponseStatuses.Rejected &&
                            (i.InvigilatorResponses
                                .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                                .OrderByDescending(r => r.ResponseAt)
                                .Select(r => r.Status)
                                .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                        .Select(i => i.NewAssignee != null
                            ? (i.NewAssignee.Faculty != null ? i.NewAssignee.Faculty.FacultyName : null)
                            : (i.Assignee.Faculty != null ? i.Assignee.Faculty.FacultyName : null))
                        .FirstOrDefault(),

                    Lecturer2Name = x.ExamInvigilators
                        .Where(i =>
                            i.PositionNo == 2 &&
                            i.Status != InvigilatorResponseStatuses.Rejected &&
                            (i.InvigilatorResponses
                                .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                                .OrderByDescending(r => r.ResponseAt)
                                .Select(r => r.Status)
                                .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                        .Select(i => i.NewAssignee != null
                            ? (i.NewAssignee.Information != null ? $"{i.NewAssignee.Information.LastName} {i.NewAssignee.Information.FirstName}" : i.NewAssignee.UserName)
                            : (i.Assignee.Information != null ? $"{i.Assignee.Information.LastName} {i.Assignee.Information.FirstName}" : i.Assignee.UserName))
                        .FirstOrDefault(),

                    Lecturer2Code = x.ExamInvigilators
                        .Where(i =>
                            i.PositionNo == 2 &&
                            i.Status != InvigilatorResponseStatuses.Rejected &&
                            (i.InvigilatorResponses
                                .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                                .OrderByDescending(r => r.ResponseAt)
                                .Select(r => r.Status)
                                .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                        .Select(i => i.NewAssignee != null ? i.NewAssignee.UserName : i.Assignee.UserName)
                        .FirstOrDefault(),

                    Lecturer2FacultyName = x.ExamInvigilators
                        .Where(i =>
                            i.PositionNo == 2 &&
                            i.Status != InvigilatorResponseStatuses.Rejected &&
                            (i.InvigilatorResponses
                                .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                                .OrderByDescending(r => r.ResponseAt)
                                .Select(r => r.Status)
                                .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                        .Select(i => i.NewAssignee != null
                            ? (i.NewAssignee.Faculty != null ? i.NewAssignee.Faculty.FacultyName : null)
                            : (i.Assignee.Faculty != null ? i.Assignee.Faculty.FacultyName : null))
                        .FirstOrDefault(),
                })
                .ToListAsync();

            return new PagedResult<ExamScheduleDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ExamScheduleDto?> GetByIdAsync(int id)
        {
            var x = await _context.ExamSchedules
                .AsNoTracking()
                .Include(e => e.AcademyYear)
                .Include(e => e.Semester)
                    .ThenInclude(s => s.AcademyYear)
                .Include(e => e.Period)
                .Include(e => e.Session)
                .Include(e => e.Slot)
                .Include(e => e.Room)
                    .ThenInclude(r => r.Building)
                .Include(e => e.ExamFormat)
                .Include(e => e.Offering)
                    .ThenInclude(o => o.Subject)
                        .ThenInclude(s => s.Faculty)
                .Include(e => e.Offering)
                    .ThenInclude(o => o.User)
                        .ThenInclude(u => u.Information)
                .Include(e => e.Offering)
                    .ThenInclude(o => o.Semester)
                        .ThenInclude(s => s.AcademyYear)
                .Include(e => e.ExamInvigilators)
                    .ThenInclude(i => i.Assignee)
                        .ThenInclude(u => u.Information)
                .Include(e => e.ExamInvigilators)
                    .ThenInclude(i => i.Assignee)
                        .ThenInclude(u => u.Faculty)
                .Include(e => e.ExamInvigilators)
                    .ThenInclude(i => i.NewAssignee)
                        .ThenInclude(u => u.Information)
                .Include(e => e.ExamInvigilators)
                    .ThenInclude(i => i.NewAssignee)
                        .ThenInclude(u => u.Faculty)
                .Include(e => e.ExamScheduleApprovals)
                .FirstOrDefaultAsync(e => e.ExamScheduleId == id);

            if (x == null) return null;

            var groupRooms = await _context.ExamSchedules
                .AsNoTracking()
                .Include(e => e.Room)
                .Where(e => e.OfferingId == x.OfferingId && e.SlotId == x.SlotId && e.ExamDate.Date == x.ExamDate.Date)
                .OrderBy(e => e.ExamScheduleId)
                .Select(e => new
                {
                    e.RoomId,
                    DisplayText = e.Room.BuildingId == "KHAC" ? e.Room.RoomName : e.Room.BuildingId + "." + e.Room.RoomName
                })
                .ToListAsync();

            return new ExamScheduleDto
            {
                Id = x.ExamScheduleId,
                OfferingId = x.OfferingId,
                SlotId = x.SlotId,
                RoomId = x.RoomId,
                ExamDate = x.ExamDate,
                Status = x.Status,
                ApprovalCount = x.ExamScheduleApprovals.Count(a => a.Status == ExamScheduleStatuses.PendingApproval),
                InvigilatorCount = x.ExamInvigilators.Count(i =>
                    i.Status != InvigilatorResponseStatuses.Rejected &&
                    (i.InvigilatorResponses
                        .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                        .OrderByDescending(r => r.ResponseAt)
                        .Select(r => r.Status)
                        .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected),
                IsLockedForScheduleEdit = x.ExamInvigilators.Any(),

                SubjectId = x.Offering?.SubjectId,
                SubjectName = x.Offering?.Subject?.SubjectName,
                Credit = x.Offering?.Subject?.Credit,
                FacultyId = x.Offering?.Subject?.FacultyId,
                FacultyName = x.Offering?.Subject?.Faculty?.FacultyName,
                ExamFormatId = x.ExamFormatId,
                ExamFormatCode = x.ExamFormat?.Code,
                ExamFormatName = x.ExamFormat?.Name,
                UserName = x.Offering?.User?.Information != null
                    ? $"{x.Offering.User.Information.LastName} {x.Offering.User.Information.FirstName}"
                    : x.Offering?.User?.UserName,
                OfferingUserCode = x.Offering?.User?.UserName,
                OfferingUserFullName = x.Offering?.User?.Information != null
                    ? $"{x.Offering.User.Information.LastName} {x.Offering.User.Information.FirstName}"
                    : x.Offering?.User?.UserName,

                ClassName = x.Offering?.ClassName,
                GroupNumber = x.Offering?.GroupNumber,

                AcademyYearId = x.AcademyYearId,
                AcademyYearName = x.AcademyYear?.AcademyYearName,

                SemesterId = x.SemesterId,
                SemesterName = x.Semester?.SemesterName,

                OfferingAcademyYearId = x.Offering?.Semester?.AcademyYearId,
                OfferingSemesterId = x.Offering?.SemesterId,

                PeriodId = x.PeriodId,
                PeriodName = x.Period?.PeriodName,

                SessionId = x.SessionId,
                SessionName = x.Session?.SessionName,

                SlotName = x.Slot != null
                    ? $"{x.Slot.SlotName} ({x.Slot.TimeStart:hh\\:mm})"
                    : null,
                SlotTimeStart = x.Slot?.TimeStart,

                BuildingId = x.Room?.BuildingId,
                BuildingName = x.Room?.Building?.BuildingName,
                RoomName = x.Room?.RoomName,
                RoomCapacity = x.Room?.Capacity,
                RoomIds = groupRooms.Select(r => r.RoomId).ToList(),
                RoomDisplayTexts = groupRooms.Select(r => r.DisplayText).ToList(),
                RoomCount = groupRooms.Count == 0 ? 1 : groupRooms.Count,

                Lecturer1Name = x.ExamInvigilators
                    .Where(i =>
                        i.PositionNo == 1 &&
                        i.Status != InvigilatorResponseStatuses.Rejected &&
                        (i.InvigilatorResponses
                            .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                            .OrderByDescending(r => r.ResponseAt)
                            .Select(r => r.Status)
                            .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                    .Select(i => i.NewAssignee != null
                        ? (i.NewAssignee.Information != null ? $"{i.NewAssignee.Information.LastName} {i.NewAssignee.Information.FirstName}" : i.NewAssignee.UserName)
                        : (i.Assignee.Information != null ? $"{i.Assignee.Information.LastName} {i.Assignee.Information.FirstName}" : i.Assignee.UserName))
                    .FirstOrDefault(),

                Lecturer1Code = x.ExamInvigilators
                    .Where(i =>
                        i.PositionNo == 1 &&
                        i.Status != InvigilatorResponseStatuses.Rejected &&
                        (i.InvigilatorResponses
                            .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                            .OrderByDescending(r => r.ResponseAt)
                            .Select(r => r.Status)
                            .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                    .Select(i => i.NewAssignee != null ? i.NewAssignee.UserName : i.Assignee.UserName)
                    .FirstOrDefault(),

                Lecturer1FacultyName = x.ExamInvigilators
                    .Where(i =>
                        i.PositionNo == 1 &&
                        i.Status != InvigilatorResponseStatuses.Rejected &&
                        (i.InvigilatorResponses
                            .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                            .OrderByDescending(r => r.ResponseAt)
                            .Select(r => r.Status)
                            .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                    .Select(i => i.NewAssignee != null
                        ? (i.NewAssignee.Faculty != null ? i.NewAssignee.Faculty.FacultyName : null)
                        : (i.Assignee.Faculty != null ? i.Assignee.Faculty.FacultyName : null))
                    .FirstOrDefault(),

                Lecturer2Name = x.ExamInvigilators
                    .Where(i =>
                        i.PositionNo == 2 &&
                        i.Status != InvigilatorResponseStatuses.Rejected &&
                        (i.InvigilatorResponses
                            .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                            .OrderByDescending(r => r.ResponseAt)
                            .Select(r => r.Status)
                            .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                    .Select(i => i.NewAssignee != null
                        ? (i.NewAssignee.Information != null ? $"{i.NewAssignee.Information.LastName} {i.NewAssignee.Information.FirstName}" : i.NewAssignee.UserName)
                        : (i.Assignee.Information != null ? $"{i.Assignee.Information.LastName} {i.Assignee.Information.FirstName}" : i.Assignee.UserName))
                    .FirstOrDefault(),

                Lecturer2Code = x.ExamInvigilators
                    .Where(i =>
                        i.PositionNo == 2 &&
                        i.Status != InvigilatorResponseStatuses.Rejected &&
                        (i.InvigilatorResponses
                            .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                            .OrderByDescending(r => r.ResponseAt)
                            .Select(r => r.Status)
                            .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                    .Select(i => i.NewAssignee != null ? i.NewAssignee.UserName : i.Assignee.UserName)
                    .FirstOrDefault(),

                Lecturer2FacultyName = x.ExamInvigilators
                    .Where(i =>
                        i.PositionNo == 2 &&
                        i.Status != InvigilatorResponseStatuses.Rejected &&
                        (i.InvigilatorResponses
                            .Where(r => r.UserId == (i.NewAssigneeId ?? i.AssigneeId))
                            .OrderByDescending(r => r.ResponseAt)
                            .Select(r => r.Status)
                            .FirstOrDefault() ?? string.Empty) != InvigilatorResponseStatuses.Rejected)
                    .Select(i => i.NewAssignee != null
                        ? (i.NewAssignee.Faculty != null ? i.NewAssignee.Faculty.FacultyName : null)
                        : (i.Assignee.Faculty != null ? i.Assignee.Faculty.FacultyName : null))
                    .FirstOrDefault(),
            };
        }

        public async Task AddAsync(ExamSchedule entity)
        {
            _context.ExamSchedules.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ExamSchedule entity)
        {
            var data = await _context.ExamSchedules.FindAsync(entity.Id);
            if (data == null) return;

            data.SlotId = entity.SlotId;
            data.AcademyYearId = entity.AcademyYearId;
            data.SemesterId = entity.SemesterId;
            data.PeriodId = entity.PeriodId;
            data.SessionId = entity.SessionId;
            data.RoomId = entity.RoomId;
            data.OfferingId = entity.OfferingId;
            data.ExamFormatId = entity.ExamFormatId;
            data.ExamDate = entity.ExamDate;
            data.Status = entity.Status;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateRoomGroupAsync(int baseScheduleId, ExamSchedule entity, List<int> roomIds)
        {
            var current = await _context.ExamSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExamScheduleId == baseScheduleId);

            if (current == null)
                throw new InvalidOperationException("Không tìm thấy lịch thi cần cập nhật.");

            var group = await _context.ExamSchedules
                .Where(x => x.OfferingId == current.OfferingId && x.SlotId == current.SlotId && x.ExamDate.Date == current.ExamDate.Date)
                .OrderBy(x => x.ExamScheduleId)
                .ToListAsync();

            for (var i = 0; i < roomIds.Count; i++)
            {
                if (i < group.Count)
                {
                    var item = group[i];
                    item.SlotId = entity.SlotId;
                    item.AcademyYearId = entity.AcademyYearId;
                    item.SemesterId = entity.SemesterId;
                    item.PeriodId = entity.PeriodId;
                    item.SessionId = entity.SessionId;
                    item.RoomId = roomIds[i];
                    item.OfferingId = entity.OfferingId;
                    item.ExamFormatId = entity.ExamFormatId;
                    item.ExamDate = entity.ExamDate;
                    item.Status = entity.Status;
                }
                else
                {
                    _context.ExamSchedules.Add(new Data.Entities.ExamSchedule
                    {
                        SlotId = entity.SlotId,
                        AcademyYearId = entity.AcademyYearId,
                        SemesterId = entity.SemesterId,
                        PeriodId = entity.PeriodId,
                        SessionId = entity.SessionId,
                        RoomId = roomIds[i],
                        OfferingId = entity.OfferingId,
                        ExamFormatId = entity.ExamFormatId,
                        ExamDate = entity.ExamDate,
                        Status = entity.Status
                    });
                }
            }

            if (group.Count > roomIds.Count)
            {
                _context.ExamSchedules.RemoveRange(group.Skip(roomIds.Count));
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var data = await _context.ExamSchedules.FindAsync(id);
            if (data != null)
            {
                _context.ExamSchedules.Remove(data);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsOfferingConflictAsync(int offeringId, int? ignoreId = null)
        {
            return await _context.ExamSchedules.AnyAsync(x =>
                x.OfferingId == offeringId &&
                (!ignoreId.HasValue || x.ExamScheduleId != ignoreId.Value));
        }

        public async Task<bool> ExistsRoomConflictAsync(int roomId, DateTime examDate, int slotId, int? ignoreId = null)
        {
            return await _context.ExamSchedules.AnyAsync(x =>
                x.RoomId == roomId &&
                x.SlotId == slotId &&
                x.ExamDate.Date == examDate.Date &&
                (!ignoreId.HasValue || x.ExamScheduleId != ignoreId.Value));
        }

        public async Task<bool> RoomExistsAsync(int roomId)
        {
            return await _context.Rooms.AnyAsync(x => x.RoomId == roomId);
        }

        public async Task<bool> ExistsRoomConflictAsync(int roomId, DateTime examDate, int slotId, IEnumerable<int> ignoreIds)
        {
            var ignored = ignoreIds.ToList();
            return await _context.ExamSchedules.AnyAsync(x =>
                x.RoomId == roomId &&
                x.SlotId == slotId &&
                x.ExamDate.Date == examDate.Date &&
                !ignored.Contains(x.ExamScheduleId));
        }

        public async Task<bool> ExamFormatExistsAsync(int examFormatId)
        {
            return await _context.ExamFormats.AnyAsync(x => x.ExamFormatId == examFormatId && x.IsActive);
        }

        public async Task<bool> HasInvigilatorsAsync(int id)
        {
            return await _context.ExamInvigilators.AnyAsync(x => x.ExamScheduleId == id);
        }

        public async Task<bool> HasInvigilatorsInRoomGroupAsync(int baseScheduleId)
        {
            var ids = await GetScheduleIdsInRoomGroupAsync(baseScheduleId);
            return await _context.ExamInvigilators.AnyAsync(x => ids.Contains(x.ExamScheduleId));
        }

        public async Task<List<int>> GetScheduleIdsInRoomGroupAsync(int baseScheduleId)
        {
            var current = await _context.ExamSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExamScheduleId == baseScheduleId);

            if (current == null) return new List<int>();

            return await _context.ExamSchedules
                .AsNoTracking()
                .Where(x => x.OfferingId == current.OfferingId && x.SlotId == current.SlotId && x.ExamDate.Date == current.ExamDate.Date)
                .Select(x => x.ExamScheduleId)
                .ToListAsync();
        }

        public async Task<List<ExamFormatDto>> GetExamFormatsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ExamFormats
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ExamFormatId)
                .Select(x => new ExamFormatDto
                {
                    Id = x.ExamFormatId,
                    Code = x.Code,
                    Name = x.Name,
                    IsActive = x.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<ExamScheduleValidationContextDto?> GetOfferingContextAsync(int offeringId)
        {
            var x = await _context.CourseOfferings
                .AsNoTracking()
                .Include(o => o.Semester)
                    .ThenInclude(s => s.AcademyYear)
                .Include(o => o.Subject)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OfferingId == offeringId);

            if (x == null) return null;

            return new ExamScheduleValidationContextDto
            {
                AcademyYearId = x.Semester?.AcademyYearId ?? 0,
                SemesterId = x.SemesterId,
                SubjectId = x.SubjectId,
                FacultyId = x.Subject?.FacultyId,
                UserId = x.UserId,
                ClassName = x.ClassName,
                GroupNumber = x.GroupNumber
            };
        }

        public async Task<ExamScheduleValidationContextDto?> GetSlotContextAsync(int slotId)
        {
            var x = await _context.ExamSlots
                .AsNoTracking()
                .Include(s => s.Session)
                    .ThenInclude(se => se.Period)
                        .ThenInclude(p => p.Semester)
                .FirstOrDefaultAsync(s => s.SlotId == slotId);

            if (x == null) return null;

            return new ExamScheduleValidationContextDto
            {
                AcademyYearId = x.Session.Period.Semester.AcademyYearId,
                SemesterId = x.Session.Period.SemesterId,
                PeriodId = x.Session.PeriodId,
                SessionId = x.SessionId
            };
        }

        public async Task MarkApprovalRequestedAsync(IEnumerable<int> scheduleIds, IEnumerable<int> approverIds, int? requestedById = null, int? facultyId = null, string? note = null, CancellationToken cancellationToken = default)
        {
            var ids = scheduleIds.Distinct().ToList();
            var deans = approverIds.Distinct().ToList();
            if (!ids.Any() || !deans.Any()) return;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var now = DateTime.Now;
            var correlationId = Guid.NewGuid();

            try
            {
                Data.Entities.ApprovalRequest? approvalRequest = null;
                if (requestedById.HasValue)
                {
                    approvalRequest = new Data.Entities.ApprovalRequest
                    {
                        RequestedById = requestedById.Value,
                        FacultyId = facultyId,
                        Status = ExamScheduleStatuses.PendingApproval,
                        Note = note,
                        CreatedAt = now,
                        CorrelationId = correlationId
                    };
                    _context.ApprovalRequests.Add(approvalRequest);
                }

                var existing = await _context.ExamScheduleApprovals
                    .Where(x => ids.Contains(x.ExamScheduleId) && deans.Contains(x.ApproverId))
                    .ToListAsync(cancellationToken);

                var existingKeys = existing
                    .Select(x => $"{x.ExamScheduleId}:{x.ApproverId}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var item in existing)
                {
                    item.Status = ExamScheduleStatuses.PendingApproval;
                    item.Note = note;
                    item.ApproveAt = null;
                    item.UpdateAt = now;
                }

                foreach (var scheduleId in ids)
                {
                    foreach (var approverId in deans)
                    {
                        if (existingKeys.Contains($"{scheduleId}:{approverId}")) continue;

                        _context.ExamScheduleApprovals.Add(new Data.Entities.ExamScheduleApproval
                        {
                            ExamScheduleId = scheduleId,
                            ApproverId = approverId,
                            Status = ExamScheduleStatuses.PendingApproval,
                            Note = note,
                            ApproveAt = null,
                            UpdateAt = now
                        });
                    }

                    if (requestedById.HasValue)
                    {
                        _context.ApprovalHistories.Add(new Data.Entities.ApprovalHistory
                        {
                            ApprovalRequest = approvalRequest,
                            ExamScheduleId = scheduleId,
                            ActorUserId = requestedById.Value,
                            FromStatus = ExamScheduleStatuses.PendingApproval,
                            ToStatus = ExamScheduleStatuses.PendingApproval,
                            Action = "RequestApproval",
                            Note = note,
                            CreatedAt = now,
                            CorrelationId = correlationId
                        });
                    }
                }

                _context.AuditLogs.Add(new Data.Entities.AuditLog
                {
                    EventType = "ApprovalRequest",
                    EntityName = "ExamSchedule",
                    EntityId = $"Schedules:{ids.Count}",
                    Action = "RequestApproval",
                    ActorUserId = requestedById,
                    NewValues = $"Schedules={ids.Count};Approvers={deans.Count};ScheduleIds={string.Join(",", ids)}",
                    Note = note,
                    CreatedAt = now,
                    CorrelationId = correlationId,
                    Source = nameof(ExamScheduleRepository)
                });

                _context.OutboxMessages.Add(new Data.Entities.OutboxMessage
                {
                    Type = "ApprovalRequestCreated",
                    Payload = $"{{\"scheduleCount\":{ids.Count},\"approverCount\":{deans.Count},\"recipientIds\":[{string.Join(",", deans)}],\"relatedId\":{approvalRequest?.ApprovalRequestId ?? 0},\"createdBy\":{requestedById ?? 0}}}",
                    Status = "Pending",
                    RetryCount = 0,
                    CreatedAt = now,
                    CorrelationId = correlationId
                });

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        private static string? GetUserFullName(dynamic? user)
        {
            if (user == null) return null;

            if (user.Information != null)
                return $"{user.Information.LastName} {user.Information.FirstName}";

            return user.UserName;
        }

            //private static string? GetInvigilatorName(IEnumerable<ExamInvigilationManagement.Infrastructure.Data.Entities.ExamInvigilator> items, byte positionNo)
            //{
            //    var inv = items
            //        .Where(x => x.PositionNo == positionNo)
            //        .OrderByDescending(x => x.UpdateAt ?? x.CreateAt)
            //        .FirstOrDefault();

            //    if (inv?.Assignee == null) return null;

            //    if (inv.Assignee.Information != null)
            //        return $"{inv.Assignee.Information.FirstName} {inv.Assignee.Information.LastName}";

            //    return inv.Assignee.UserName;
            //}

        private static string? GetInvigilatorName(
    IEnumerable<ExamInvigilationManagement.Infrastructure.Data.Entities.ExamInvigilator> items,
    byte positionNo)
        {
            var inv = items.FirstOrDefault(x => x.PositionNo == positionNo);
            if (inv?.Assignee == null) return null;

            return inv.Assignee.Information != null
                ? $"{inv.Assignee.Information.LastName} {inv.Assignee.Information.FirstName}"
                : inv.Assignee.UserName;
        }
    }
}
