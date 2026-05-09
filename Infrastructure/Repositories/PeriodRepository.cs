using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class PeriodRepository : IPeriodRepository
    {
        private readonly ApplicationDbContext _context;

        public PeriodRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<ExamPeriod>> GetAllBySemesterAsync(int semesterId)
        {
           return await _context.ExamPeriods
                .Where(p => p.SemesterId == semesterId)
                .Select(p => p.ToDomain())
                .ToListAsync();
        }
        public async Task<ExamPeriod?> GetByIdAsync(int id)
        {
            var data =  await _context.ExamPeriods
                .FirstOrDefaultAsync(x => x.PeriodId == id);
            return data?.ToDomain();
        }
        public async Task AddAsync(int semesterId, ExamPeriod entity)
        {
            entity.SemesterId = semesterId;

            _context.ExamPeriods.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, string name)
        {
            var entity = await _context.ExamPeriods.FindAsync(id);
            if (entity == null) return;

            entity.PeriodName = name;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.ExamPeriods.FindAsync(id);
            if (entity == null) return;

            _context.ExamPeriods.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
