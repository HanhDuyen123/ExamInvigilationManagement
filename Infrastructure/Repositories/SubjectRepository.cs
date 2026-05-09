using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class SubjectRepository : ISubjectRepository
    {
        private readonly ApplicationDbContext _context;

        public SubjectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Subject>> GetPagedAsync(
            string? id,
            string? name,
            byte? credit,
            int? facultyId,
            int page,
            int pageSize)
        {
            var query = _context.Subjects
                .AsNoTracking()
                .Include(x => x.Faculty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(id))
            {
                var keyword = id.Trim().ToUpperInvariant();
                query = query.Where(x => x.SubjectId.ToUpper().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var keyword = name.Trim();
                query = query.Where(x => x.SubjectName.Contains(keyword));
            }

            if (credit.HasValue)
                query = query.Where(x => x.Credit == credit.Value);

            if (facultyId.HasValue)
                query = query.Where(x => x.FacultyId == facultyId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.SubjectId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.ToDomain())
                .ToListAsync();

            return new PagedResult<Subject>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<Subject>> GetAllAsync()
        {
            return await _context.Subjects
                .AsNoTracking()
                .Include(x => x.Faculty)
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<Subject?> GetByIdAsync(string id)
        {
            var normalized = (id ?? string.Empty).Trim().ToUpperInvariant();

            var entity = await _context.Subjects
                .AsNoTracking()
                .Include(x => x.Faculty)
                .FirstOrDefaultAsync(x => x.SubjectId == normalized);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByIdAsync(string id)
        {
            var normalized = (id ?? string.Empty).Trim().ToUpperInvariant();

            return await _context.Subjects
                .AsNoTracking()
                .AnyAsync(x => x.SubjectId == normalized);
        }

        public async Task<bool> FacultyExistsAsync(int facultyId)
        {
            return await _context.Faculties
                .AsNoTracking()
                .AnyAsync(x => x.FacultyId == facultyId);
        }

        public async Task<bool> HasCourseOfferingsAsync(string subjectId)
        {
            var normalized = (subjectId ?? string.Empty).Trim().ToUpperInvariant();

            return await _context.CourseOfferings
                .AsNoTracking()
                .AnyAsync(x => x.SubjectId == normalized);
        }

        public async Task AddAsync(Subject entity)
        {
            _context.Subjects.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Subject entity)
        {
            var data = await _context.Subjects.FindAsync(entity.Id);
            if (data == null)
                throw new InvalidOperationException("Không tìm thấy môn học cần cập nhật.");

            data.SubjectName = entity.Name;
            data.FacultyId = entity.FacultyId;
            data.Credit = entity.Credit;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var normalized = (id ?? string.Empty).Trim().ToUpperInvariant();

            var data = await _context.Subjects.FindAsync(normalized);
            if (data != null)
            {
                _context.Subjects.Remove(data);
                await _context.SaveChangesAsync();
            }
        }
    }
}