using ExamInvigilationManagement.Application.DTOs.Statistics;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Services
{
    public class CurrentAcademicContextService : ICurrentAcademicContextService
    {
        private readonly ApplicationDbContext _db;

        public CurrentAcademicContextService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CurrentAcademicContextDto?> GetCurrentContextAsync(int userId, string roleName, int? facultyId = null, CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var configuredCurrent = await _db.Semesters
                .AsNoTracking()
                .Where(x => x.StartDate.HasValue && x.EndDate.HasValue && x.StartDate.Value <= today && x.EndDate.Value >= today)
                .OrderByDescending(x => x.StartDate)
                .Select(x => new CurrentAcademicContextDto
                {
                    AcademyYearId = x.AcademyYearId,
                    AcademyYearName = x.AcademyYear.AcademyYearName,
                    SemesterId = x.SemesterId,
                    SemesterName = x.SemesterName,
                    PeriodId = null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (configuredCurrent is not null)
                return configuredCurrent;

            var userFacultyId = await _db.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.FacultyId)
                .FirstOrDefaultAsync(cancellationToken);

            var isLecturer = roleName.Equals("Giảng viên", StringComparison.OrdinalIgnoreCase);
            var isFacultyScope = roleName.Equals("Thư ký khoa", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Trưởng khoa", StringComparison.OrdinalIgnoreCase);
            var isAdmin = roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            var schedules = _db.ExamSchedules.AsNoTracking().AsQueryable();
            if (isLecturer)
                schedules = schedules.Where(x => x.Status == "Đã duyệt" && x.ExamInvigilators.Any(i => i.AssigneeId == userId));
            if (isFacultyScope && userFacultyId.HasValue)
                schedules = schedules.Where(x => x.Offering.Subject.FacultyId == userFacultyId.Value);
            if (isAdmin && facultyId.HasValue)
                schedules = schedules.Where(x => x.Offering.Subject.FacultyId == facultyId.Value);

            return await schedules
                .OrderBy(x => Math.Abs(EF.Functions.DateDiffDay(today, x.ExamDate)))
                .ThenByDescending(x => x.ExamDate)
                .Select(x => new CurrentAcademicContextDto
                {
                    AcademyYearId = x.AcademyYearId,
                    AcademyYearName = x.AcademyYear.AcademyYearName,
                    SemesterId = x.SemesterId,
                    SemesterName = x.Semester.SemesterName,
                    PeriodId = x.PeriodId,
                    PeriodName = x.Period.PeriodName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
