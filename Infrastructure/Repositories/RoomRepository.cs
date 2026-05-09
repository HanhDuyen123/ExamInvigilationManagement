using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly ApplicationDbContext _context;

        public RoomRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Room>> GetAllAsync()
        {
            return await _context.Rooms
                .AsNoTracking()
                .Include(x => x.Building)
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<Room?> GetByIdAsync(int id)
        {
            var entity = await _context.Rooms
                .AsNoTracking()
                .Include(x => x.Building)
                .FirstOrDefaultAsync(x => x.RoomId == id);

            return entity?.ToDomain();
        }

        public async Task<bool> BuildingExistsAsync(string buildingId)
        {
            return await _context.Buildings
                .AsNoTracking()
                .AnyAsync(x => x.BuildingId == buildingId);
        }

        public async Task<bool> ExistsByBuildingAndRoomNameAsync(string buildingId, string roomName, int? excludeRoomId = null)
        {
            var query = _context.Rooms.AsNoTracking()
                .Where(x => x.BuildingId == buildingId && x.RoomName == roomName);

            if (excludeRoomId.HasValue)
                query = query.Where(x => x.RoomId != excludeRoomId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> HasExamSchedulesAsync(int roomId)
        {
            return await _context.ExamSchedules
                .AsNoTracking()
                .AnyAsync(x => x.RoomId == roomId);
        }

        public async Task AddAsync(Room entity)
        {
            await _context.Rooms.AddAsync(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Room entity)
        {
            var dbEntity = await _context.Rooms.FindAsync(entity.Id);
            if (dbEntity == null)
                throw new InvalidOperationException("Không tìm thấy phòng cần cập nhật.");

            dbEntity.BuildingId = entity.BuildingId;
            dbEntity.RoomName = entity.Name;
            dbEntity.Capacity = entity.Capacity;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Rooms.FindAsync(id);
            if (entity != null)
            {
                _context.Rooms.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}