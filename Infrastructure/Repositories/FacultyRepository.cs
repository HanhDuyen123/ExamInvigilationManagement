using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class FacultyRepository : IFacultyRepository
    {
        private readonly ApplicationDbContext _context;

        public FacultyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Faculty>> GetAllAsync()
        {
            return await _context.Faculties
                .AsNoTracking()
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<Faculty?> GetByIdAsync(int id)
        {
            var entity = await _context.Faculties
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FacultyId == id);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var query = _context.Faculties.AsNoTracking()
                .Where(x => x.FacultyName == name);

            if (excludeId.HasValue)
                query = query.Where(x => x.FacultyId != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> HasUsersAsync(int id)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.FacultyId == id);
        }

        public async Task<bool> HasSubjectsAsync(int id)
        {
            return await _context.Subjects
                .AsNoTracking()
                .AnyAsync(x => x.FacultyId == id);
        }

        public async Task AddAsync(Faculty entity)
        {
            _context.Faculties.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Faculty entity)
        {
            var data = await _context.Faculties.FindAsync(entity.Id);
            if (data == null)
                throw new InvalidOperationException("Không tìm thấy khoa cần cập nhật.");

            data.FacultyName = entity.Name;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var data = await _context.Faculties.FindAsync(id);
            if (data == null)
                throw new InvalidOperationException("Không tìm thấy khoa cần xóa.");

            _context.Faculties.Remove(data);
            await _context.SaveChangesAsync();
        }
    }
}