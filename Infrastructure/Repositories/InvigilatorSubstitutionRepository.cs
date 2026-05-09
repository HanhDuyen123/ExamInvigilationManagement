using ExamInvigilationManagement.Application.DTOs.InvigilatorSubstitution;
using ExamInvigilationManagement.Application.DTOs.ManualAssignment;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class InvigilatorSubstitutionRepository : IInvigilatorSubstitutionRepository
    {
        private readonly ApplicationDbContext _db;

        public InvigilatorSubstitutionRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<int?> GetUserFacultyIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return _db.Users.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.FacultyId).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<InvigilatorSubstitutionScheduleDto?> GetRejectedAssignmentAsync(int examInvigilatorId, int userId, CancellationToken cancellationToken = default)
        {
            return BuildAssignmentQuery()
                .Where(x => x.ExamInvigilatorId == examInvigilatorId && x.CurrentAssigneeId == userId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<InvigilatorSubstitutionScheduleDto?> GetAssignmentForReviewAsync(int examInvigilatorId, CancellationToken cancellationToken = default)
        {
            return BuildAssignmentQuery().FirstOrDefaultAsync(x => x.ExamInvigilatorId == examInvigilatorId, cancellationToken);
        }

        public async Task<List<ManualAssignmentLecturerOptionDto>> GetActiveLecturersAsync(int facultyId, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x => x.IsActive && x.FacultyId == facultyId && x.Role.RoleName == "Giảng viên")
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

        public async Task<List<(int UserId, string DisplayName)>> GetActiveSecretariesAsync(int facultyId, CancellationToken cancellationToken = default)
        {
            return await _db.Users.AsNoTracking()
                .Where(x => x.IsActive && x.FacultyId == facultyId && x.Role.RoleName == "Thư ký khoa")
                .Select(x => new ValueTuple<int, string>(x.UserId, x.Information == null ? x.UserName : x.Information.LastName + " " + x.Information.FirstName))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<int>> GetBusyLecturerIdsAsync(IEnumerable<int> userIds, int slotId, DateOnly examDate, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();
            return await _db.LecturerBusySlots.AsNoTracking()
                .Where(x => ids.Contains(x.UserId) && x.SlotId == slotId && x.BusyDate == examDate)
                .Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
        }

        public async Task<List<int>> GetConflictingLecturerIdsAsync(int scheduleId, int semesterId, int periodId, int sessionId, int slotId, IEnumerable<int> userIds, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();
            var matchingAssignments = _db.ExamInvigilators.AsNoTracking()
                .Where(x => x.ExamScheduleId != scheduleId && x.ExamSchedule.SemesterId == semesterId && x.ExamSchedule.PeriodId == periodId && x.ExamSchedule.SessionId == sessionId && x.ExamSchedule.SlotId == slotId && ids.Contains(x.AssigneeId))
                .Select(x => x.AssigneeId);
            var matchingReplacements = _db.ExamInvigilators.AsNoTracking()
                .Where(x => x.ExamScheduleId != scheduleId && x.ExamSchedule.SemesterId == semesterId && x.ExamSchedule.PeriodId == periodId && x.ExamSchedule.SessionId == sessionId && x.ExamSchedule.SlotId == slotId && x.NewAssigneeId.HasValue && ids.Contains(x.NewAssigneeId.Value))
                .Select(x => x.NewAssigneeId!.Value);

            return await matchingAssignments.Concat(matchingReplacements).Distinct().ToListAsync(cancellationToken);
        }

        public Task<Dictionary<int, int>> GetLecturerLoadsAsync(int semesterId, int facultyId, CancellationToken cancellationToken = default)
        {
            return _db.ExamInvigilators.AsNoTracking()
                .Where(x => x.ExamSchedule.SemesterId == semesterId && x.Assignee.FacultyId == facultyId && x.Assignee.IsActive)
                .GroupBy(x => x.AssigneeId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
        }

        public Task<Dictionary<int, int>> GetPeriodLoadsAsync(int semesterId, int periodId, int facultyId, CancellationToken cancellationToken = default)
        {
            return _db.ExamInvigilators.AsNoTracking()
                .Where(x => x.ExamSchedule.SemesterId == semesterId && x.ExamSchedule.PeriodId == periodId && x.Assignee.FacultyId == facultyId && x.Assignee.IsActive)
                .GroupBy(x => x.AssigneeId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
        }

        public Task<Dictionary<int, int>> GetSameDayLoadsAsync(int semesterId, int facultyId, DateTime examDate, CancellationToken cancellationToken = default)
        {
            return _db.ExamInvigilators.AsNoTracking()
                .Where(x => x.ExamSchedule.SemesterId == semesterId && x.Assignee.FacultyId == facultyId && x.ExamSchedule.ExamDate == examDate)
                .GroupBy(x => x.AssigneeId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
        }

        public async Task<List<int>> GetSubjectTeacherIdsAsync(int semesterId, string subjectId, int facultyId, CancellationToken cancellationToken = default)
        {
            return await _db.CourseOfferings.AsNoTracking()
                .Where(x => x.SemesterId == semesterId && x.SubjectId == subjectId && x.User.FacultyId == facultyId && x.User.IsActive)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task<List<int>> GetClassTeacherIdsAsync(int semesterId, string subjectId, string className, string groupNumber, int facultyId, CancellationToken cancellationToken = default)
        {
            return await _db.CourseOfferings.AsNoTracking()
                .Where(x => x.SemesterId == semesterId && x.SubjectId == subjectId && x.ClassName == className && x.GroupNumber == groupNumber && x.User.FacultyId == facultyId && x.User.IsActive)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public Task<bool> HasAnyAsync(int examInvigilatorId, int userId, CancellationToken cancellationToken = default)
        {
            return _db.InvigilatorSubstitutions.AsNoTracking()
                .AnyAsync(x => x.ExamInvigilatorId == examInvigilatorId && x.UserId == userId, cancellationToken);
        }

        public async Task<int> CreateAsync(int examInvigilatorId, int userId, int substituteUserId, CancellationToken cancellationToken = default)
        {
            var entity = new Data.Entities.InvigilatorSubstitution
            {
                ExamInvigilatorId = examInvigilatorId,
                UserId = userId,
                SubstituteUserId = substituteUserId,
                Status = "Đã đề xuất",
                CreateAt = DateTime.Now
            };
            _db.InvigilatorSubstitutions.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return entity.SubstitutionId;
        }

        public async Task<PagedResult<InvigilatorSubstitutionListItemDto>> GetPagedAsync(int facultyId, InvigilatorSubstitutionSearchDto search, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var query = BuildListQuery(facultyId);
            query = ApplySearch(query, search);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(x => x.CreateAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<InvigilatorSubstitutionListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<InvigilatorSubstitutionDetailDto?> GetDetailAsync(int substitutionId, int facultyId, CancellationToken cancellationToken = default)
        {
            return await _db.InvigilatorSubstitutions.AsNoTracking()
                .Where(x => x.SubstitutionId == substitutionId && x.ExamInvigilator.ExamSchedule.Offering.Subject.FacultyId == facultyId)
                .Select(x => new InvigilatorSubstitutionDetailDto
                {
                    SubstitutionId = x.SubstitutionId,
                    ExamInvigilatorId = x.ExamInvigilatorId,
                    ExamScheduleId = x.ExamInvigilator.ExamScheduleId,
                    SubjectId = x.ExamInvigilator.ExamSchedule.Offering.SubjectId,
                    SubjectName = x.ExamInvigilator.ExamSchedule.Offering.Subject.SubjectName,
                    ClassName = x.ExamInvigilator.ExamSchedule.Offering.ClassName,
                    GroupNumber = x.ExamInvigilator.ExamSchedule.Offering.GroupNumber,
                    ExamDate = x.ExamInvigilator.ExamSchedule.ExamDate,
                    SlotName = x.ExamInvigilator.ExamSchedule.Slot.SlotName,
                    TimeStart = x.ExamInvigilator.ExamSchedule.Slot.TimeStart,
                    RoomDisplay = x.ExamInvigilator.ExamSchedule.Room.BuildingId + "." + x.ExamInvigilator.ExamSchedule.Room.RoomName,
                    PositionNo = x.ExamInvigilator.PositionNo,
                    RequestUserName = x.User.Information.LastName + " " + x.User.Information.FirstName,
                    RequestUserAccount = x.User.UserName,
                    SubstituteUserId = x.SubstituteUserId,
                    SubstituteUserName = x.SubstituteUser.Information.LastName + " " + x.SubstituteUser.Information.FirstName,
                    SubstituteUserAccount = x.SubstituteUser.UserName,
                    Status = x.Status,
                    CreateAt = x.CreateAt,
                    ScheduleStatus = x.ExamInvigilator.ExamSchedule.Status,
                    ResponseStatus = x.ExamInvigilator.InvigilatorResponses.Where(r => r.UserId == x.UserId).OrderByDescending(r => r.ResponseAt).Select(r => r.Status).FirstOrDefault() ?? "Chưa phản hồi",
                    ResponseNote = x.ExamInvigilator.InvigilatorResponses.Where(r => r.UserId == x.UserId).OrderByDescending(r => r.ResponseAt).Select(r => r.Note).FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task ApproveAsync(int substitutionId, int reviewerId, CancellationToken cancellationToken = default)
        {
            var substitution = await _db.InvigilatorSubstitutions
                .Include(x => x.ExamInvigilator)
                .FirstOrDefaultAsync(x => x.SubstitutionId == substitutionId, cancellationToken);
            if (substitution == null) throw new InvalidOperationException("Không tìm thấy đề xuất thay thế.");

            var scheduleId = substitution.ExamInvigilator.ExamScheduleId;
            substitution.Status = "Đã duyệt";
            substitution.ExamInvigilator.NewAssigneeId = substitution.SubstituteUserId;
            substitution.ExamInvigilator.UpdateAt = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            await MarkSchedulePendingApprovalAsync(scheduleId, cancellationToken);
        }

        public async Task ApproveWithReplacementAsync(int substitutionId, int replacementUserId, int reviewerId, CancellationToken cancellationToken = default)
        {
            var substitution = await _db.InvigilatorSubstitutions
                .Include(x => x.ExamInvigilator)
                .FirstOrDefaultAsync(x => x.SubstitutionId == substitutionId, cancellationToken);
            if (substitution == null) throw new InvalidOperationException("Không tìm thấy đề xuất thay thế.");

            var scheduleId = substitution.ExamInvigilator.ExamScheduleId;
            substitution.SubstituteUserId = replacementUserId;
            substitution.Status = "Đã duyệt";
            substitution.ExamInvigilator.NewAssigneeId = replacementUserId;
            substitution.ExamInvigilator.UpdateAt = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            await MarkSchedulePendingApprovalAsync(scheduleId, cancellationToken);
        }

        public async Task RejectAsync(int substitutionId, CancellationToken cancellationToken = default)
        {
            var substitution = await _db.InvigilatorSubstitutions.FirstOrDefaultAsync(x => x.SubstitutionId == substitutionId, cancellationToken);
            if (substitution == null) throw new InvalidOperationException("Không tìm thấy đề xuất thay thế.");
            substitution.Status = "Từ chối duyệt";
            await _db.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<InvigilatorSubstitutionScheduleDto> BuildAssignmentQuery()
        {
            return _db.ExamInvigilators.AsNoTracking().Select(x => new InvigilatorSubstitutionScheduleDto
            {
                ExamInvigilatorId = x.ExamInvigilatorId,
                ExamScheduleId = x.ExamScheduleId,
                PositionNo = x.PositionNo,
                CurrentAssigneeId = x.AssigneeId,
                CurrentAssigneeName = x.Assignee.Information.LastName + " " + x.Assignee.Information.FirstName,
                FacultyId = x.ExamSchedule.Offering.Subject.FacultyId,
                SubjectId = x.ExamSchedule.Offering.SubjectId,
                SubjectName = x.ExamSchedule.Offering.Subject.SubjectName,
                ClassName = x.ExamSchedule.Offering.ClassName,
                GroupNumber = x.ExamSchedule.Offering.GroupNumber,
                ExamDate = x.ExamSchedule.ExamDate,
                SemesterId = x.ExamSchedule.SemesterId,
                PeriodId = x.ExamSchedule.PeriodId,
                SessionId = x.ExamSchedule.SessionId,
                SlotId = x.ExamSchedule.SlotId,
                OfferingUserId = x.ExamSchedule.Offering.UserId,
                SlotName = x.ExamSchedule.Slot.SlotName,
                TimeStart = x.ExamSchedule.Slot.TimeStart,
                RoomDisplay = x.ExamSchedule.Room.BuildingId + "." + x.ExamSchedule.Room.RoomName,
                ScheduleStatus = x.ExamSchedule.Status,
                ResponseStatus = x.InvigilatorResponses.Where(r => r.UserId == x.AssigneeId).OrderByDescending(r => r.ResponseAt).Select(r => r.Status).FirstOrDefault() ?? "Chưa phản hồi",
                ResponseNote = x.InvigilatorResponses.Where(r => r.UserId == x.AssigneeId).OrderByDescending(r => r.ResponseAt).Select(r => r.Note).FirstOrDefault()
            });
        }

        private IQueryable<InvigilatorSubstitutionListItemDto> BuildListQuery(int facultyId)
        {
            return _db.InvigilatorSubstitutions.AsNoTracking()
                .Where(x => x.ExamInvigilator.ExamSchedule.Offering.Subject.FacultyId == facultyId)
                .Select(x => new InvigilatorSubstitutionListItemDto
                {
                    SubstitutionId = x.SubstitutionId,
                    ExamInvigilatorId = x.ExamInvigilatorId,
                    ExamScheduleId = x.ExamInvigilator.ExamScheduleId,
                    SubjectId = x.ExamInvigilator.ExamSchedule.Offering.SubjectId,
                    SubjectName = x.ExamInvigilator.ExamSchedule.Offering.Subject.SubjectName,
                    ClassName = x.ExamInvigilator.ExamSchedule.Offering.ClassName,
                    GroupNumber = x.ExamInvigilator.ExamSchedule.Offering.GroupNumber,
                    ExamDate = x.ExamInvigilator.ExamSchedule.ExamDate,
                    SlotName = x.ExamInvigilator.ExamSchedule.Slot.SlotName,
                    TimeStart = x.ExamInvigilator.ExamSchedule.Slot.TimeStart,
                    RoomDisplay = x.ExamInvigilator.ExamSchedule.Room.BuildingId + "." + x.ExamInvigilator.ExamSchedule.Room.RoomName,
                    PositionNo = x.ExamInvigilator.PositionNo,
                    RequestUserName = x.User.Information.LastName + " " + x.User.Information.FirstName,
                    SubstituteUserName = x.SubstituteUser.Information.LastName + " " + x.SubstituteUser.Information.FirstName,
                    Status = x.Status,
                    CreateAt = x.CreateAt
                });
        }

        private static IQueryable<InvigilatorSubstitutionListItemDto> ApplySearch(IQueryable<InvigilatorSubstitutionListItemDto> query, InvigilatorSubstitutionSearchDto search)
        {
            if (!string.IsNullOrWhiteSpace(search.Status)) query = query.Where(x => x.Status == search.Status);
            if (search.FromDate.HasValue) query = query.Where(x => x.ExamDate >= search.FromDate.Value.Date);
            if (search.ToDate.HasValue) query = query.Where(x => x.ExamDate <= search.ToDate.Value.Date);
            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var keyword = search.Keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.SubjectId.ToLower().Contains(keyword) ||
                    x.SubjectName.ToLower().Contains(keyword) ||
                    x.ClassName.ToLower().Contains(keyword) ||
                    x.RequestUserName.ToLower().Contains(keyword) ||
                    x.SubstituteUserName.ToLower().Contains(keyword));
            }
            return query;
        }

        private async Task MarkSchedulePendingApprovalAsync(int scheduleId, CancellationToken cancellationToken)
        {
            var schedule = await _db.ExamSchedules.FirstOrDefaultAsync(x => x.ExamScheduleId == scheduleId, cancellationToken);
            if (schedule == null) throw new InvalidOperationException("Không tìm thấy lịch thi cần cập nhật trạng thái.");
            schedule.Status = "Chờ duyệt";
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
