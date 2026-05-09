using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly ApplicationDbContext _context;

        public SessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<ExamSession>> GetAllByPeriodAsync(int periodId)
        {
            return await _context.ExamSessions
                .Where(s => s.PeriodId == periodId)
                .Select(s => s.ToDomain())
                .ToListAsync();
        }
        public async Task<ExamSession?> GetByIdAsync(int id)
        {
            var data = await _context.ExamSessions
                .FirstOrDefaultAsync(x => x.SessionId == id);
            return data?.ToDomain();
        }
        public async Task AddAsync(int periodId, ExamSession entity)
        {
            entity.PeriodId = periodId;

            _context.ExamSessions.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, string name)
        {
            var entity = await _context.ExamSessions.FindAsync(id);
            if (entity == null) return;

            entity.SessionName = name;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.ExamSessions.FindAsync(id);
            if (entity == null) return;

            _context.ExamSessions.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
