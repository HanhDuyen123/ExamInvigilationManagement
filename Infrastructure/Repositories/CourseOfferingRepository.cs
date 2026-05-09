using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class CourseOfferingRepository : ICourseOfferingRepository
    {
        private readonly ApplicationDbContext _context;

        public CourseOfferingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CourseOffering>> GetAllAsync()
        {
            return await _context.CourseOfferings
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(u => u.Information)
                .Include(x => x.User)
                    .ThenInclude(u => u.Faculty)
                .Include(x => x.Semester)
                    .ThenInclude(s => s.AcademyYear)
                .Include(x => x.Subject)
                    .ThenInclude(s => s.Faculty)
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<CourseOffering?> GetByIdAsync(int id)
        {
            var entity = await _context.CourseOfferings
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(u => u.Information)
                .Include(x => x.User)
                    .ThenInclude(u => u.Faculty)
                .Include(x => x.Semester)
                    .ThenInclude(s => s.AcademyYear)
                .Include(x => x.Subject)
                    .ThenInclude(s => s.Faculty)
                .FirstOrDefaultAsync(x => x.OfferingId == id);

            return entity?.ToDomain();
        }

        public async Task AddAsync(CourseOffering entity)
        {
            await _context.CourseOfferings.AddAsync(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CourseOffering entity)
        {
            var data = await _context.CourseOfferings.FindAsync(entity.Id);
            if (data == null) return;

            data.UserId = entity.UserId;
            data.SemesterId = entity.SemesterId;
            data.SubjectId = entity.SubjectId;
            data.ClassName = entity.ClassName;
            data.GroupNumber = entity.GroupNumber;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var data = await _context.CourseOfferings.FindAsync(id);
            if (data != null)
            {
                _context.CourseOfferings.Remove(data);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            return await _context.CourseOfferings.AsNoTracking()
                .AnyAsync(x => x.OfferingId == id);
        }

        public async Task<bool> ExistsByUniqueKeyAsync(
            int semesterId,
            string subjectId,
            string className,
            string groupNumber,
            int? excludeId = null)
        {
            var query = _context.CourseOfferings.AsNoTracking().Where(x =>
                x.SemesterId == semesterId &&
                x.SubjectId == subjectId &&
                x.ClassName == className &&
                x.GroupNumber == groupNumber);

            if (excludeId.HasValue)
                query = query.Where(x => x.OfferingId != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> HasExamSchedulesAsync(int offeringId)
        {
            return await _context.ExamSchedules
                .AsNoTracking()
                .AnyAsync(x => x.OfferingId == offeringId);
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId);
        }

        public async Task<bool> IsUserActiveLecturerAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.IsActive &&
                    x.Role.RoleName == "Giảng viên");
        }

        public async Task<int?> GetUserFacultyIdAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.FacultyId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> SubjectExistsAsync(string subjectId)
        {
            return await _context.Subjects
                .AsNoTracking()
                .AnyAsync(x => x.SubjectId == subjectId);
        }

        public async Task<int?> GetSubjectFacultyIdAsync(string subjectId)
        {
            return await _context.Subjects
                .AsNoTracking()
                .Where(x => x.SubjectId == subjectId)
                .Select(x => (int?)x.FacultyId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> SemesterExistsAsync(int semesterId)
        {
            return await _context.Semesters
                .AsNoTracking()
                .AnyAsync(x => x.SemesterId == semesterId);
        }

        public async Task<int?> GetSemesterAcademyYearIdAsync(int semesterId)
        {
            return await _context.Semesters
                .AsNoTracking()
                .Where(x => x.SemesterId == semesterId)
                .Select(x => (int?)x.AcademyYearId)
                .FirstOrDefaultAsync();
        }
    }
}