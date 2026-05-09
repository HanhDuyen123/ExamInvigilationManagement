using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class PositionRepository : IPositionRepository
    {
        private readonly ApplicationDbContext _context;

        public PositionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Position>> GetAllAsync()
        {
            return await _context.Positions
                .AsNoTracking()
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<Position?> GetByIdAsync(byte id)
        {
            var entity = await _context.Positions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PositionId == id);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByNameAsync(string name, byte? excludeId = null)
        {
            var query = _context.Positions.AsNoTracking().Where(x => x.PositionName == name);

            if (excludeId.HasValue)
                query = query.Where(x => x.PositionId != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> HasInformationAsync(byte id)
        {
            return await _context.Information
                .AsNoTracking()
                .AnyAsync(x => x.PositionId == id);
        }

        public async Task AddAsync(Position position)
        {
            var entity = position.ToEntity();
            _context.Positions.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Position position)
        {
            var entity = await _context.Positions.FindAsync(position.Id);
            if (entity == null)
                throw new InvalidOperationException("Không tìm thấy chức vụ cần cập nhật.");

            entity.PositionName = position.Name;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(byte id)
        {
            var entity = await _context.Positions.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException("Không tìm thấy chức vụ cần xóa.");

            _context.Positions.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}