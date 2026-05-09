using ExamInvigilationManagement.Application.DTOs.Approval;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class ExamScheduleApprovalRepository : IExamScheduleApprovalRepository
    {
        private const string StatusWaiting = "Chờ duyệt";

        private readonly ApplicationDbContext _db;

        public ExamScheduleApprovalRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ApprovalUserContextDto?> GetUserContextAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new ApprovalUserContextDto
                {
                    UserId = x.UserId,
                    FacultyId = x.FacultyId,
                    RoleName = x.Role.RoleName,
                    FullName = x.Information.LastName + " " + x.Information.FirstName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<ExamScheduleApprovalPageResultDto> GetIndexPageAsync(
    int facultyId,
    ExamScheduleApprovalSearchDto search,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
        {
            var query = _db.ExamSchedules
                .AsNoTracking()
                .Where(x => x.Offering.Subject.FacultyId == facultyId);

            if (search.AcademyYearId.HasValue)
                query = query.Where(x => x.AcademyYearId == search.AcademyYearId.Value);

            if (search.SemesterId.HasValue)
                query = query.Where(x => x.SemesterId == search.SemesterId.Value);

            if (search.PeriodId.HasValue)
                query = query.Where(x => x.PeriodId == search.PeriodId.Value);

            if (search.SessionId.HasValue)
                query = query.Where(x => x.SessionId == search.SessionId.Value);

            if (search.SlotId.HasValue)
                query = query.Where(x => x.SlotId == search.SlotId.Value);

            if (!string.IsNullOrWhiteSpace(search.SubjectId))
                query = query.Where(x => x.Offering.SubjectId == search.SubjectId);

            if (search.LecturerId.HasValue)
                query = query.Where(x => x.ExamInvigilators.Any(i => i.AssigneeId == search.LecturerId.Value));

            if (!string.IsNullOrWhiteSpace(search.BuildingId))
                query = query.Where(x => x.Room.BuildingId == search.BuildingId);

            if (search.RoomId.HasValue)
                query = query.Where(x => x.RoomId == search.RoomId.Value);

            if (string.IsNullOrWhiteSpace(search.Status) || search.Status == "Chờ duyệt")
                query = query.Where(x => x.Status == StatusWaiting);
            else if (!string.Equals(search.Status, "Tất cả", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Status == search.Status.Trim());

            var baseRows = await query
                .Select(x => new
                {
                    x.ExamScheduleId,
                    x.Offering.SubjectId,
                    SubjectName = x.Offering.Subject.SubjectName,
                    x.Offering.ClassName,
                    x.Offering.GroupNumber,

                    x.AcademyYearId,
                    AcademyYearName = x.AcademyYear.AcademyYearName,

                    x.SemesterId,
                    SemesterName = x.Semester.SemesterName,

                    x.PeriodId,
                    PeriodName = x.Period.PeriodName,

                    x.SessionId,
                    SessionName = x.Session.SessionName,

                    x.SlotId,
                    SlotName = x.Slot.SlotName,
                    TimeStart = x.Slot.TimeStart,

                    x.RoomId,
                    BuildingId = x.Room.BuildingId,
                    BuildingName = x.Room.Building.BuildingName,
                    RoomName = x.Room.RoomName,
                    RoomDisplay = x.Room.BuildingId + "." + x.Room.RoomName,

                    x.ExamDate,
                    x.Status,

                    InvigilatorCount = x.ExamInvigilators.Count,
                    ApprovalCount = x.ExamScheduleApprovals.Count
                })
                .OrderBy(x => x.ExamDate)
                .ThenBy(x => x.TimeStart)
                .ToListAsync(cancellationToken);

            var scheduleIds = baseRows.Select(x => x.ExamScheduleId).ToList();

            var invigilators = await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x => scheduleIds.Contains(x.ExamScheduleId))
                .Select(x => new
                {
                    x.ExamScheduleId,
                    x.PositionNo,
                    x.AssigneeId,
                    FullName = x.Assignee.Information.LastName + " " + x.Assignee.Information.FirstName
                })
                .ToListAsync(cancellationToken);

            var invLookup = invigilators
                .GroupBy(x => x.ExamScheduleId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var allItems = baseRows.Select(x =>
            {
                invLookup.TryGetValue(x.ExamScheduleId, out var list);
                var gt1 = list?.FirstOrDefault(i => i.PositionNo == 1);
                var gt2 = list?.FirstOrDefault(i => i.PositionNo == 2);

                var isFinal = IsFinalStatus(x.Status);
                var hasEnoughInvigilators = x.InvigilatorCount >= 2;

                var canReview = !isFinal && hasEnoughInvigilators;

                var reviewReason = isFinal
                    ? $"Lịch thi đang ở trạng thái '{x.Status}'."
                    : !hasEnoughInvigilators
                        ? "Lịch thi chưa đủ 2 giám thị."
                        : string.Empty;

                return new ExamScheduleApprovalIndexItemDto
                {
                    ExamScheduleId = x.ExamScheduleId,
                    SubjectId = x.SubjectId,
                    SubjectName = x.SubjectName,
                    ClassName = x.ClassName,
                    GroupNumber = x.GroupNumber,

                    AcademyYearId = x.AcademyYearId,
                    AcademyYearName = x.AcademyYearName,

                    SemesterId = x.SemesterId,
                    SemesterName = x.SemesterName,

                    PeriodId = x.PeriodId,
                    PeriodName = x.PeriodName,

                    SessionId = x.SessionId,
                    SessionName = x.SessionName,

                    SlotId = x.SlotId,
                    SlotName = x.SlotName,
                    TimeStart = x.TimeStart,

                    RoomId = x.RoomId,
                    BuildingId = x.BuildingId,
                    BuildingName = x.BuildingName,
                    RoomName = x.RoomName,
                    RoomDisplay = x.RoomDisplay,

                    ExamDate = x.ExamDate,

                    Invigilator1Id = gt1?.AssigneeId,
                    Invigilator2Id = gt2?.AssigneeId,
                    Invigilator1Name = gt1?.FullName ?? string.Empty,
                    Invigilator2Name = gt2?.FullName ?? string.Empty,

                    CurrentInvigilatorCount = x.InvigilatorCount,
                    ApprovalCount = x.ApprovalCount,

                    Status = x.Status,
                    CanReview = canReview,
                    ReviewReason = reviewReason
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search.Lookup))
            {
                var key = search.Lookup.Trim();
                allItems = allItems.Where(x =>
                    x.Invigilator1Name.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.Invigilator2Name.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.SubjectId.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.SubjectName.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.ClassName.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.RoomDisplay.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.RoomName.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.BuildingId.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.BuildingName.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    x.Status.Contains(key, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var totalCount = allItems.Count(x => !IsFinalStatus(x.Status));
            var reviewableCount = allItems.Count(x => x.CanReview);
            var notEnoughCount = allItems.Count(x => !IsFinalStatus(x.Status) && x.CurrentInvigilatorCount < 2);

            var pagedItems = allItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new ExamScheduleApprovalPageResultDto
            {
                TotalCount = totalCount,
                ReviewableCount = reviewableCount,
                NotEnoughCount = notEnoughCount,
                PagedItems = new PagedResult<ExamScheduleApprovalIndexItemDto>
                {
                    Items = pagedItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            };
        }

        public async Task<List<ExamScheduleApprovalIndexItemDto>> GetBulkTargetsAsync(
            int facultyId,
            IEnumerable<int> examScheduleIds,
            CancellationToken cancellationToken = default)
        {
            var ids = examScheduleIds.Distinct().ToList();
            var search = new ExamScheduleApprovalSearchDto
            {
                Status = "Tất cả"
            };

            var page = await GetIndexPageAsync(facultyId, search, 1, int.MaxValue, cancellationToken);
            return page.PagedItems.Items.Where(x => ids.Contains(x.ExamScheduleId)).ToList();
        }

        public async Task<List<int>> GetSecretaryRecipientIdsAsync(
            int facultyId,
            int excludeUserId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.FacultyId == facultyId &&
                    x.Role.RoleName == "Thư ký khoa" &&
                    x.UserId != excludeUserId)
                .OrderBy(x => x.UserId)
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveBulkAsync(
            ExamScheduleApprovalSavePlanDto plan,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var scheduleIds = plan.Items.Select(x => x.ExamScheduleId).Distinct().ToList();

                var schedules = await _db.ExamSchedules
                    .Where(x => scheduleIds.Contains(x.ExamScheduleId))
                    .ToListAsync(cancellationToken);

                if (schedules.Count != scheduleIds.Count)
                    throw new InvalidOperationException("Có lịch thi không tồn tại.");

                var scheduleMap = schedules.ToDictionary(x => x.ExamScheduleId);

                foreach (var item in plan.Items)
                {
                    var schedule = scheduleMap[item.ExamScheduleId];

                    if (IsFinalStatus(schedule.Status))
                        throw new InvalidOperationException($"Lịch thi #{item.ExamScheduleId} đã ở trạng thái cuối, không thể duyệt.");

                    var invigilatorCount = await _db.ExamInvigilators
                        .CountAsync(x => x.ExamScheduleId == item.ExamScheduleId, cancellationToken);

                    if (invigilatorCount < 2)
                        throw new InvalidOperationException($"Lịch thi #{item.ExamScheduleId} chưa đủ 2 giám thị.");

                    _db.ExamScheduleApprovals.Add(new Data.Entities.ExamScheduleApproval
                    {
                        ExamScheduleId = item.ExamScheduleId,
                        ApproverId = item.ApproverId,
                        Status = item.Status,
                        Note = item.Note,
                        ApproveAt = item.ApproveAt,
                        UpdateAt = item.UpdateAt
                    });

                    schedule.Status = item.Status;
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
        private static bool IsFinalStatus(string status)
        {
            return status.Equals("Đã duyệt", StringComparison.OrdinalIgnoreCase)
                   || status.Equals("Từ chối duyệt", StringComparison.OrdinalIgnoreCase);
        }
    }
}