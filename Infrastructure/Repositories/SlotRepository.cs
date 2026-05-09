using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class SlotRepository : ISlotRepository
    {
        private readonly ApplicationDbContext _context;

        public SlotRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<ExamSlot>> GetAllBySessionAsync(int sessionId)
        {
            return await _context.ExamSlots
                .Where(s => s.SessionId == sessionId)
                .Select(s => s.ToDomain())
                .ToListAsync();
        }
        public async Task<ExamSlot?> GetByIdAsync(int id)
        {
            var entity = await _context.ExamSlots.FindAsync(id);
            if (entity == null) return null;

            return new ExamSlot
            {
                Id = entity.SlotId,
                SessionId = entity.SessionId,
                Name = entity.SlotName,
                TimeStart = entity.TimeStart
            };
        }
        public async Task AddAsync(int sessionId, ExamSlot entity)
        {
            entity.SessionId = sessionId;

            _context.ExamSlots.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ExamSlot slot)
        {
            var entity = await _context.ExamSlots.FindAsync(slot.Id);
            if (entity == null) return;

            entity.SlotName = slot.Name;
            entity.TimeStart = slot.TimeStart;

            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(int id, string name, TimeOnly timeStart)
        {
            var entity = await _context.ExamSlots.FindAsync(id);
            if (entity == null) return;

            entity.SlotName = name;
            entity.TimeStart = timeStart;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.ExamSlots.FindAsync(id);
            if (entity == null) return;

            _context.ExamSlots.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
