using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Domain.Enums;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class SemesterRepository : ISemesterRepository
    {
        private readonly ApplicationDbContext _context;

        public SemesterRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<Semester>> GetAllAsync()
        {
            return await _context.Semesters
                .Select(x => x.ToDomain())
                .ToListAsync();
        }
        public async Task<Semester?> GetByIdAsync(int id)
        {
            var data = await _context.Semesters
                .FirstOrDefaultAsync(x => x.SemesterId == id);
            return data?.ToDomain();
        }
        public async Task AddAsync(int academyYearId, Semester entity)
        {
            entity.AcademyYearId = academyYearId;

            _context.Semesters.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, string name)
        {
            var entity = await _context.Semesters.FindAsync(id);
            if (entity == null) return;

            entity.SemesterName = name;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Semesters.FindAsync(id);
            if (entity == null) return;

            _context.Semesters.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
