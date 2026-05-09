using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class BuildingRepository : IBuildingRepository
    {
        private readonly ApplicationDbContext _context;

        public BuildingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Building>> GetAllAsync()
        {
            return await _context.Buildings
                .AsNoTracking()
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<Building?> GetByIdAsync(string id)
        {
            var entity = await _context.Buildings.FindAsync(id);
            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByIdAsync(string id)
        {
            return await _context.Buildings
                .AsNoTracking()
                .AnyAsync(x => x.BuildingId == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, string? excludeId = null)
        {
            var query = _context.Buildings.AsNoTracking().Where(x => x.BuildingName == name);

            if (!string.IsNullOrWhiteSpace(excludeId))
                query = query.Where(x => x.BuildingId != excludeId);

            return await query.AnyAsync();
        }

        public async Task<bool> HasRoomsAsync(string id)
        {
            return await _context.Rooms
                .AsNoTracking()
                .AnyAsync(x => x.BuildingId == id);
        }

        public async Task AddAsync(Building entity)
        {
            await _context.Buildings.AddAsync(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Building entity)
        {
            var dbEntity = await _context.Buildings.FindAsync(entity.Id);

            if (dbEntity == null)
                throw new Exception("Không tìm thấy giảng đường.");

            dbEntity.BuildingName = entity.Name;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await _context.Buildings.FindAsync(id);
            if (entity != null)
            {
                _context.Buildings.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}