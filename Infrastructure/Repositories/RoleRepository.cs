using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await _context.Roles
                .AsNoTracking()
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<Role?> GetByIdAsync(byte id)
        {
            var entity = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.RoleId == id);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByNameAsync(string name, byte? excludeId = null)
        {
            var query = _context.Roles.AsNoTracking().Where(x => x.RoleName == name);

            if (excludeId.HasValue)
                query = query.Where(x => x.RoleId != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> HasUsersAsync(byte id)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.RoleId == id);
        }

        public async Task AddAsync(Role role)
        {
            var entity = role.ToEntity();
            _context.Roles.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Role role)
        {
            var entity = await _context.Roles.FindAsync(role.Id);
            if (entity == null)
                throw new InvalidOperationException("Không tìm thấy vai trò cần cập nhật.");

            entity.RoleName = role.Name;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(byte id)
        {
            var entity = await _context.Roles.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException("Không tìm thấy vai trò cần xóa.");

            _context.Roles.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}