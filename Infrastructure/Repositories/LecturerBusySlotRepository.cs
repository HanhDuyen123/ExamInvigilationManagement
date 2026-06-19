using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Constants;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;
using DataEntities = ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class LecturerBusySlotRepository : ILecturerBusySlotRepository
    {
        private readonly ApplicationDbContext _context;

        public LecturerBusySlotRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<LecturerBusySlotDto>> GetPagedAsync(LecturerBusySlotSearchDto filter, int page, int pageSize)
        {
            var slotQuery = _context.LecturerBusySlots
                .AsNoTracking()
                .Include(x => x.User).ThenInclude(x => x.Information)
                .Include(x => x.User).ThenInclude(x => x.Faculty)
                .Include(x => x.ApprovedBy).ThenInclude(x => x!.Information)
                .Include(x => x.Slot)
                    .ThenInclude(x => x.Session)
                        .ThenInclude(x => x.Period)
                            .ThenInclude(x => x.Semester)
                                .ThenInclude(x => x.AcademyYear)
                .AsQueryable();

            var periodQuery = _context.LecturerBusyPeriods
                .AsNoTracking()
                .Include(x => x.User).ThenInclude(x => x.Information)
                .Include(x => x.User).ThenInclude(x => x.Faculty)
                .Include(x => x.ApprovedBy).ThenInclude(x => x!.Information)
                .Include(x => x.Period).ThenInclude(x => x.Semester).ThenInclude(x => x.AcademyYear)
                .AsQueryable();

            if (filter.UserId.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.UserId == filter.UserId.Value);
                periodQuery = periodQuery.Where(x => x.UserId == filter.UserId.Value);
            }

            if (filter.FacultyId.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.User.FacultyId == filter.FacultyId.Value);
                periodQuery = periodQuery.Where(x => x.User.FacultyId == filter.FacultyId.Value);
            }

            if (filter.AcademyYearId.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.Slot.Session.Period.Semester.AcademyYearId == filter.AcademyYearId.Value);
                periodQuery = periodQuery.Where(x => x.Period.Semester.AcademyYearId == filter.AcademyYearId.Value);
            }

            if (filter.SemesterId.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.Slot.Session.Period.Semester.SemesterId == filter.SemesterId.Value);
                periodQuery = periodQuery.Where(x => x.Period.SemesterId == filter.SemesterId.Value);
            }

            if (filter.ExamPeriodId.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.Slot.Session.Period.PeriodId == filter.ExamPeriodId.Value);
                periodQuery = periodQuery.Where(x => x.PeriodId == filter.ExamPeriodId.Value);
            }

            if (filter.ExamSessionId.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.Slot.Session.SessionId == filter.ExamSessionId.Value);
                periodQuery = periodQuery.Where(x => x.Period.ExamSessions.Any(s => s.SessionId == filter.ExamSessionId.Value));
            }

            if (filter.ExamSlotId.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.SlotId == filter.ExamSlotId.Value);
                periodQuery = periodQuery.Where(x => x.Period.ExamSessions.Any(s => s.ExamSlots.Any(slot => slot.SlotId == filter.ExamSlotId.Value)));
            }

            if (filter.FromDate.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.BusyDate >= filter.FromDate.Value);
                var fromDate = filter.FromDate.Value.ToDateTime(TimeOnly.MinValue);
                periodQuery = periodQuery.Where(x => x.Period.ExamSchedules.Any(s => s.ExamDate >= fromDate));
            }

            if (filter.ToDate.HasValue)
            {
                slotQuery = slotQuery.Where(x => x.BusyDate <= filter.ToDate.Value);
                var toDate = filter.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
                periodQuery = periodQuery.Where(x => x.Period.ExamSchedules.Any(s => s.ExamDate <= toDate));
            }

            if (!string.IsNullOrWhiteSpace(filter.ApprovalStatus))
            {
                slotQuery = slotQuery.Where(x => x.ApprovalStatus == filter.ApprovalStatus);
                periodQuery = periodQuery.Where(x => x.ApprovalStatus == filter.ApprovalStatus);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();

                slotQuery = slotQuery.Where(x =>
                    (x.Note ?? "").ToLower().Contains(kw) ||
                    (x.User.UserName ?? "").ToLower().Contains(kw) ||
                    ((x.User.Information != null
                        ? (x.User.Information.FirstName + " " + x.User.Information.LastName)
                        : "")).ToLower().Contains(kw) ||
                    (x.User.Faculty != null && (x.User.Faculty.FacultyName ?? "").ToLower().Contains(kw)) ||
                    (x.Slot.Session.Period.Semester.AcademyYear.AcademyYearName ?? "").ToLower().Contains(kw) ||
                    (x.Slot.Session.Period.Semester.SemesterName ?? "").ToLower().Contains(kw) ||
                    (x.Slot.Session.Period.PeriodName ?? "").ToLower().Contains(kw) ||
                    (x.Slot.Session.SessionName ?? "").ToLower().Contains(kw) ||
                    (x.Slot.SlotName ?? "").ToLower().Contains(kw)
                );

                periodQuery = periodQuery.Where(x =>
                    (x.Note ?? "").ToLower().Contains(kw) ||
                    (x.User.UserName ?? "").ToLower().Contains(kw) ||
                    ((x.User.Information != null
                        ? (x.User.Information.FirstName + " " + x.User.Information.LastName)
                        : "")).ToLower().Contains(kw) ||
                    (x.User.Faculty != null && (x.User.Faculty.FacultyName ?? "").ToLower().Contains(kw)) ||
                    (x.Period.Semester.AcademyYear.AcademyYearName ?? "").ToLower().Contains(kw) ||
                    (x.Period.Semester.SemesterName ?? "").ToLower().Contains(kw) ||
                    (x.Period.PeriodName ?? "").ToLower().Contains(kw)
                );
            }

            var slotItems = await slotQuery
                .Select(x => new LecturerBusySlotDto
                {
                    Id = x.BusySlotId,
                    UserId = x.UserId,
                    UserName = x.User.Information != null
                        ? $"{x.User.Information.LastName} {x.User.Information.FirstName}"
                        : x.User.UserName,

                    FacultyId = x.User.FacultyId,
                    FacultyName = x.User.Faculty != null ? x.User.Faculty.FacultyName : null,

                    AcademyYearId = x.Slot.Session.Period.Semester.AcademyYearId,
                    AcademyYearName = x.Slot.Session.Period.Semester.AcademyYear.AcademyYearName,

                    SemesterId = x.Slot.Session.Period.Semester.SemesterId,
                    SemesterName = x.Slot.Session.Period.Semester.SemesterName,

                    ExamPeriodId = x.Slot.Session.Period.PeriodId,
                    ExamPeriodName = x.Slot.Session.Period.PeriodName,

                    ExamSessionId = x.Slot.Session.SessionId,
                    ExamSessionName = x.Slot.Session.SessionName,

                    ExamSlotId = x.SlotId,
                    ExamSlotName = x.Slot.SlotName + " (" + x.Slot.TimeStart.ToString("HH\\:mm") + ")",

                    BusyDate = x.BusyDate,
                    Note = x.Note,
                    CreateAt = x.CreateAt,
                    ApprovalStatus = x.ApprovalStatus,
                    ApprovedById = x.ApprovedById,
                    ApprovedByName = x.ApprovedBy == null ? null : x.ApprovedBy.Information.LastName + " " + x.ApprovedBy.Information.FirstName,
                    ApprovedAt = x.ApprovedAt,
                    RejectionReason = x.RejectionReason,
                    BusyWholePeriod = false
                })
                .ToListAsync();

            var periodRows = await periodQuery
                .Select(x => new LecturerBusySlotDto
                {
                    Id = -x.BusyPeriodId,
                    UserId = x.UserId,
                    UserName = x.User.Information != null ? x.User.Information.LastName + " " + x.User.Information.FirstName : x.User.UserName,
                    FacultyId = x.User.FacultyId,
                    FacultyName = x.User.Faculty != null ? x.User.Faculty.FacultyName : null,
                    AcademyYearId = x.Period.Semester.AcademyYearId,
                    AcademyYearName = x.Period.Semester.AcademyYear.AcademyYearName,
                    SemesterId = x.Period.Semester.SemesterId,
                    SemesterName = x.Period.Semester.SemesterName,
                    ExamPeriodId = x.PeriodId,
                    ExamPeriodName = x.Period.PeriodName,
                    ExamSessionName = "Cả đợt",
                    ExamSlotName = "Cả đợt",
                    Note = x.Note,
                    CreateAt = x.CreateAt,
                    ApprovalStatus = x.ApprovalStatus,
                    ApprovedById = x.ApprovedById,
                    ApprovedByName = x.ApprovedBy == null ? null : x.ApprovedBy.Information.LastName + " " + x.ApprovedBy.Information.FirstName,
                    ApprovedAt = x.ApprovedAt,
                    RejectionReason = x.RejectionReason,
                    BusyWholePeriod = true
                })
                .ToListAsync();

            var periodItems = periodRows
                .Select(x =>
                {
                    x.BusyDate = default;
                    return x;
                })
                .ToList();

            var combined = slotItems.Concat(periodItems)
                .OrderByDescending(x => x.CreateAt ?? DateTime.MinValue)
                .ThenByDescending(x => Math.Abs(x.Id))
                .ToList();

            var total = combined.Count;
            var items = combined.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult<LecturerBusySlotDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<LecturerBusySlotDto?> GetByIdAsync(int id)
        {
            if (id < 0)
            {
                var periodId = Math.Abs(id);
                var p = await _context.LecturerBusyPeriods
                    .AsNoTracking()
                    .Include(x => x.User).ThenInclude(x => x.Information)
                    .Include(x => x.User).ThenInclude(x => x.Faculty)
                    .Include(x => x.ApprovedBy).ThenInclude(x => x!.Information)
                    .Include(x => x.Period).ThenInclude(x => x.Semester).ThenInclude(x => x.AcademyYear)
                    .FirstOrDefaultAsync(x => x.BusyPeriodId == periodId);

                if (p == null) return null;

                return new LecturerBusySlotDto
                {
                    Id = -p.BusyPeriodId,
                    UserId = p.UserId,
                    UserName = p.User.Information != null ? p.User.Information.LastName + " " + p.User.Information.FirstName : p.User.UserName,
                    FacultyId = p.User.FacultyId,
                    FacultyName = p.User.Faculty != null ? p.User.Faculty.FacultyName : null,
                    AcademyYearId = p.Period.Semester.AcademyYearId,
                    AcademyYearName = p.Period.Semester.AcademyYear.AcademyYearName,
                    SemesterId = p.Period.Semester.SemesterId,
                    SemesterName = p.Period.Semester.SemesterName,
                    ExamPeriodId = p.PeriodId,
                    ExamPeriodName = p.Period.PeriodName,
                    ExamSessionName = "Cả đợt",
                    ExamSlotName = "Cả đợt",
                    BusyDate = default,
                    Note = p.Note,
                    CreateAt = p.CreateAt,
                    ApprovalStatus = p.ApprovalStatus,
                    ApprovedById = p.ApprovedById,
                    ApprovedByName = p.ApprovedBy == null ? null : p.ApprovedBy.Information.LastName + " " + p.ApprovedBy.Information.FirstName,
                    ApprovedAt = p.ApprovedAt,
                    RejectionReason = p.RejectionReason,
                    BusyWholePeriod = true
                };
            }

            var x = await _context.LecturerBusySlots
                .AsNoTracking()
                .Include(x => x.User).ThenInclude(x => x.Information)
                .Include(x => x.User).ThenInclude(x => x.Faculty)
                .Include(x => x.ApprovedBy).ThenInclude(x => x!.Information)
                .Include(x => x.Slot)
                    .ThenInclude(x => x.Session)
                        .ThenInclude(x => x.Period)
                            .ThenInclude(x => x.Semester)
                                .ThenInclude(x => x.AcademyYear)
                .FirstOrDefaultAsync(x => x.BusySlotId == id);

            if (x == null) return null;

            return new LecturerBusySlotDto
            {
                Id = x.BusySlotId,
                UserId = x.UserId,
                UserName = x.User.Information != null
                    ? $"{x.User.Information.LastName} {x.User.Information.FirstName}"
                    : x.User.UserName,

                FacultyId = x.User.FacultyId,
                FacultyName = x.User.Faculty != null ? x.User.Faculty.FacultyName : null,

                AcademyYearId = x.Slot.Session.Period.Semester.AcademyYearId,
                AcademyYearName = x.Slot.Session.Period.Semester.AcademyYear.AcademyYearName,

                SemesterId = x.Slot.Session.Period.Semester.SemesterId,
                SemesterName = x.Slot.Session.Period.Semester.SemesterName,

                ExamPeriodId = x.Slot.Session.Period.PeriodId,
                ExamPeriodName = x.Slot.Session.Period.PeriodName,

                ExamSessionId = x.Slot.Session.SessionId,
                ExamSessionName = x.Slot.Session.SessionName,

                ExamSlotId = x.SlotId,
                ExamSlotName = x.Slot.SlotName + " (" + x.Slot.TimeStart.ToString("HH\\:mm") + ")",

                BusyDate = x.BusyDate,
                Note = x.Note,
                CreateAt = x.CreateAt,
                ApprovalStatus = x.ApprovalStatus,
                ApprovedById = x.ApprovedById,
                ApprovedByName = x.ApprovedBy == null ? null : x.ApprovedBy.Information.LastName + " " + x.ApprovedBy.Information.FirstName,
                ApprovedAt = x.ApprovedAt,
                RejectionReason = x.RejectionReason,
                BusyWholePeriod = false
            };
        }

        public async Task AddAsync(LecturerBusySlot entity)
        {
            if (string.IsNullOrWhiteSpace(entity.ApprovalStatus)) entity.ApprovalStatus = BusyApprovalStatuses.Pending;
            _context.LecturerBusySlots.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<LecturerBusySlot> entities)
        {
            foreach (var entity in entities)
                if (string.IsNullOrWhiteSpace(entity.ApprovalStatus)) entity.ApprovalStatus = BusyApprovalStatuses.Pending;
            _context.LecturerBusySlots.AddRange(entities.Select(x => x.ToEntity()));
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LecturerBusySlot entity)
        {
            var data = await _context.LecturerBusySlots.FindAsync(entity.Id);
            if (data == null) return;

            data.UserId = entity.UserId;
            data.SlotId = entity.SlotId;
            data.BusyDate = entity.BusyDate;
            data.Note = entity.Note;
            data.CreateAt = entity.CreateAt;
            data.ApprovalStatus = entity.ApprovalStatus;
            data.ApprovedById = entity.ApprovedById;
            data.ApprovedAt = entity.ApprovedAt;
            data.RejectionReason = entity.RejectionReason;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            if (id < 0)
            {
                var period = await _context.LecturerBusyPeriods.FindAsync(Math.Abs(id));
                if (period != null)
                {
                    _context.LecturerBusyPeriods.Remove(period);
                    await _context.SaveChangesAsync();
                }
                return;
            }

            var data = await _context.LecturerBusySlots.FindAsync(id);
            if (data != null)
            {
                _context.LecturerBusySlots.Remove(data);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddBusyPeriodAsync(int userId, int periodId, string note)
        {
            _context.LecturerBusyPeriods.Add(new Data.Entities.LecturerBusyPeriod
            {
                UserId = userId,
                PeriodId = periodId,
                Note = note,
                CreateAt = DateTime.Now,
                ApprovalStatus = BusyApprovalStatuses.Pending
            });
            await _context.SaveChangesAsync();
        }

        public Task<bool> BusyPeriodExistsAsync(int userId, int periodId)
        {
            return _context.LecturerBusyPeriods.AnyAsync(x => x.UserId == userId && x.PeriodId == periodId);
        }

        public async Task ApproveAsync(int id, int approverId)
        {
            if (id < 0)
            {
                var period = await _context.LecturerBusyPeriods.FindAsync(Math.Abs(id));
                if (period == null) throw new InvalidOperationException("Không tìm thấy lịch bận.");
                period.ApprovalStatus = BusyApprovalStatuses.Approved;
                period.ApprovedById = approverId;
                period.ApprovedAt = DateTime.Now;
                period.RejectionReason = null;
                await _context.SaveChangesAsync();
                return;
            }

            var slot = await _context.LecturerBusySlots.FindAsync(id);
            if (slot == null) throw new InvalidOperationException("Không tìm thấy lịch bận.");
            slot.ApprovalStatus = BusyApprovalStatuses.Approved;
            slot.ApprovedById = approverId;
            slot.ApprovedAt = DateTime.Now;
            slot.RejectionReason = null;
            await _context.SaveChangesAsync();
        }

        public async Task RejectAsync(int id, int approverId, string reason)
        {
            if (id < 0)
            {
                var period = await _context.LecturerBusyPeriods.FindAsync(Math.Abs(id));
                if (period == null) throw new InvalidOperationException("Không tìm thấy lịch bận.");
                period.ApprovalStatus = BusyApprovalStatuses.Rejected;
                period.ApprovedById = approverId;
                period.ApprovedAt = DateTime.Now;
                period.RejectionReason = reason;
                await _context.SaveChangesAsync();
                return;
            }

            var slot = await _context.LecturerBusySlots.FindAsync(id);
            if (slot == null) throw new InvalidOperationException("Không tìm thấy lịch bận.");
            slot.ApprovalStatus = BusyApprovalStatuses.Rejected;
            slot.ApprovedById = approverId;
            slot.ApprovedAt = DateTime.Now;
            slot.RejectionReason = reason;
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<LecturerPeriodAvailabilityDto>> GetAvailabilityPagedAsync(LecturerPeriodAvailabilitySearchDto filter, int page, int pageSize)
        {
            var period = filter.PeriodId.HasValue
                ? await _context.ExamPeriods
                    .AsNoTracking()
                    .Where(x => x.PeriodId == filter.PeriodId.Value)
                    .Select(x => new
                    {
                        x.PeriodId,
                        x.PeriodName,
                        x.Semester.SemesterName,
                        x.Semester.AcademyYear.AcademyYearName
                    })
                    .FirstOrDefaultAsync()
                : null;

            var query = _context.Users
                .AsNoTracking()
                .Include(x => x.Information)
                .Include(x => x.Faculty)
                .Where(x => x.IsActive && x.Role.RoleName == "Giảng viên")
                .AsQueryable();

            if (filter.FacultyId.HasValue) query = query.Where(x => x.FacultyId == filter.FacultyId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.UserName.ToLower().Contains(kw) ||
                    ((x.Information != null ? x.Information.LastName + " " + x.Information.FirstName : "").ToLower().Contains(kw)) ||
                    ((x.Faculty != null ? x.Faculty.FacultyName : "").ToLower().Contains(kw)));
            }

            var periodId = period?.PeriodId ?? 0;
            var periodName = period?.PeriodName ?? string.Empty;
            var semesterName = period?.SemesterName ?? string.Empty;
            var academyYearName = period?.AcademyYearName ?? string.Empty;
            var total = await query.CountAsync();
            var lecturers = await query
                .OrderBy(x => x.Information != null ? x.Information.LastName : string.Empty)
                .ThenBy(x => x.Information != null ? x.Information.FirstName : string.Empty)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LecturerPeriodAvailabilityDto
                {
                    UserId = x.UserId,
                    UserName = x.UserName,
                    FullName = x.Information != null ? x.Information.LastName + " " + x.Information.FirstName : x.UserName,
                    FacultyId = x.FacultyId,
                    FacultyName = x.Faculty != null ? x.Faculty.FacultyName : null,
                    PeriodId = periodId,
                    PeriodName = periodName,
                    SemesterName = semesterName,
                    AcademyYearName = academyYearName
                })
                .ToListAsync();

            if (period != null)
            {
                var lecturerIds = lecturers.Select(x => x.UserId).ToList();
                var availabilityMap = await _context.LecturerPeriodAvailabilities
                    .AsNoTracking()
                    .Where(x => x.PeriodId == period.PeriodId && lecturerIds.Contains(x.UserId))
                    .Select(x => new { x.AvailabilityId, x.UserId, x.Note, x.Source, x.CreatedAt })
                    .ToDictionaryAsync(x => x.UserId);

                foreach (var lecturer in lecturers)
                {
                    if (!availabilityMap.TryGetValue(lecturer.UserId, out var availability)) continue;

                    lecturer.Id = availability.AvailabilityId;
                    lecturer.IsSelected = true;
                    lecturer.Note = availability.Note;
                    lecturer.Source = availability.Source;
                    lecturer.CreatedAt = availability.CreatedAt;
                }
            }

            return new PagedResult<LecturerPeriodAvailabilityDto>
            {
                Items = lecturers,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task SetPeriodAvailabilityAsync(int userId, int periodId, bool isAvailable, int currentUserId, int? facultyScopeId = null)
        {
            var lecturer = await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (lecturer == null || !lecturer.IsActive || !string.Equals(lecturer.Role.RoleName, "Giảng viên", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Không tìm thấy giảng viên hợp lệ.");

            if (facultyScopeId.HasValue && lecturer.FacultyId != facultyScopeId.Value)
                throw new InvalidOperationException("Bạn không có quyền cập nhật giảng viên ngoài khoa.");

            var periodExists = await _context.ExamPeriods.AnyAsync(x => x.PeriodId == periodId);
            if (!periodExists)
                throw new InvalidOperationException("Đợt thi không tồn tại.");

            var existing = await _context.LecturerPeriodAvailabilities
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PeriodId == periodId);

            if (isAvailable)
            {
                if (existing == null)
                {
                    _context.LecturerPeriodAvailabilities.Add(new DataEntities.LecturerPeriodAvailability
                    {
                        UserId = userId,
                        PeriodId = periodId,
                        Source = "UI",
                        CreatedById = currentUserId,
                        CreatedAt = DateTime.Now
                    });
                }
            }
            else if (existing != null)
            {
                _context.LecturerPeriodAvailabilities.Remove(existing);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> GetDeanIdsForLecturerAsync(int lecturerUserId, CancellationToken cancellationToken = default)
        {
            var facultyId = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == lecturerUserId)
                .Select(x => x.FacultyId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!facultyId.HasValue) return new List<int>();

            return await _context.Users
                .AsNoTracking()
                .Where(x => x.IsActive && x.FacultyId == facultyId.Value && x.Role.RoleName == "Trưởng khoa")
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GetLecturerDisplayNameAsync(int lecturerUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == lecturerUserId)
                .Select(x => x.Information != null
                    ? x.Information.LastName + " " + x.Information.FirstName
                    : x.UserName)
                .FirstOrDefaultAsync(cancellationToken) ?? $"Giảng viên #{lecturerUserId}";
        }

        public async Task<bool> IsPeriodInExpiredSemesterAsync(int periodId, DateTime today)
        {
            return await _context.ExamPeriods
                .AsNoTracking()
                .AnyAsync(x => x.PeriodId == periodId && x.Semester.EndDate.HasValue && x.Semester.EndDate.Value < today.Date);
        }

        public async Task<bool> AnySlotInExpiredSemesterAsync(List<int> slotIds, DateTime today)
        {
            return await _context.ExamSlots
                .AsNoTracking()
                .AnyAsync(x => slotIds.Contains(x.SlotId) && x.Session.Period.Semester.EndDate.HasValue && x.Session.Period.Semester.EndDate.Value < today.Date);
        }

        public async Task<bool> HasEffectiveAssignmentForLecturerPeriodAsync(int lecturerUserId, int periodId)
        {
            var facultyId = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == lecturerUserId)
                .Select(x => x.FacultyId)
                .FirstOrDefaultAsync();

            if (!facultyId.HasValue) return false;

            return await HasEffectiveAssignmentForFacultyPeriodAsync(facultyId.Value, periodId);
        }

        public async Task<bool> HasEffectiveAssignmentForLecturerSlotAsync(int lecturerUserId, int slotId)
        {
            var data = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == lecturerUserId)
                .Select(x => new { x.FacultyId })
                .FirstOrDefaultAsync();

            if (data?.FacultyId == null) return false;

            var periodId = await _context.ExamSlots
                .AsNoTracking()
                .Where(x => x.SlotId == slotId)
                .Select(x => (int?)x.Session.PeriodId)
                .FirstOrDefaultAsync();

            return periodId.HasValue && await HasEffectiveAssignmentForFacultyPeriodAsync(data.FacultyId.Value, periodId.Value);
        }

        private Task<bool> HasEffectiveAssignmentForFacultyPeriodAsync(int facultyId, int periodId)
        {
            return _context.ExamInvigilators
                .AsNoTracking()
                .AnyAsync(x =>
                    x.ExamSchedule.PeriodId == periodId &&
                    x.Status != ExamInvigilatorStatuses.Rejected &&
                    x.Status != ExamInvigilatorStatuses.RejectedCode &&
                    x.Status != ExamInvigilatorStatuses.Cancelled &&
                    x.Status != ExamInvigilatorStatuses.CancelledCode &&
                    (x.Assignee.FacultyId == facultyId ||
                     (x.NewAssigneeId.HasValue && x.NewAssignee != null && x.NewAssignee.FacultyId == facultyId)));
        }

        public async Task<bool> ExistsAsync(int userId, int slotId, DateOnly busyDate, int? ignoreId = null)
        {
            return await _context.LecturerBusySlots.AnyAsync(x =>
                x.UserId == userId &&
                x.SlotId == slotId &&
                x.BusyDate == busyDate &&
                (!ignoreId.HasValue || x.BusySlotId != ignoreId.Value));
        }
    }
}
