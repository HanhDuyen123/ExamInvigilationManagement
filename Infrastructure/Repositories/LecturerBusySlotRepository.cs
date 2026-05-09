using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

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
            var query = _context.LecturerBusySlots
                .AsNoTracking()
                .Include(x => x.User).ThenInclude(x => x.Information)
                .Include(x => x.User).ThenInclude(x => x.Faculty)
                .Include(x => x.Slot)
                    .ThenInclude(x => x.Session)
                        .ThenInclude(x => x.Period)
                            .ThenInclude(x => x.Semester)
                                .ThenInclude(x => x.AcademyYear)
                .AsQueryable();

            if (filter.UserId.HasValue)
                query = query.Where(x => x.UserId == filter.UserId.Value);

            if (filter.FacultyId.HasValue)
                query = query.Where(x => x.User.FacultyId == filter.FacultyId.Value);

            if (filter.AcademyYearId.HasValue)
                query = query.Where(x => x.Slot.Session.Period.Semester.AcademyYearId == filter.AcademyYearId.Value);

            if (filter.SemesterId.HasValue)
                query = query.Where(x => x.Slot.Session.Period.Semester.SemesterId == filter.SemesterId.Value);

            if (filter.ExamPeriodId.HasValue)
                query = query.Where(x => x.Slot.Session.Period.PeriodId == filter.ExamPeriodId.Value);

            if (filter.ExamSessionId.HasValue)
                query = query.Where(x => x.Slot.Session.SessionId == filter.ExamSessionId.Value);

            if (filter.ExamSlotId.HasValue)
                query = query.Where(x => x.SlotId == filter.ExamSlotId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.BusyDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.BusyDate <= filter.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();

                query = query.Where(x =>
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
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.BusyDate)
                .ThenByDescending(x => x.BusySlotId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                    CreateAt = x.CreateAt
                })
                .ToListAsync();

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
            var x = await _context.LecturerBusySlots
                .AsNoTracking()
                .Include(x => x.User).ThenInclude(x => x.Information)
                .Include(x => x.User).ThenInclude(x => x.Faculty)
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
                CreateAt = x.CreateAt
            };
        }

        public async Task AddAsync(LecturerBusySlot entity)
        {
            _context.LecturerBusySlots.Add(entity.ToEntity());
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

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var data = await _context.LecturerBusySlots.FindAsync(id);
            if (data != null)
            {
                _context.LecturerBusySlots.Remove(data);
                await _context.SaveChangesAsync();
            }
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
