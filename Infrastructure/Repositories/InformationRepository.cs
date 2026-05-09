using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class InformationRepository : IInformationRepository
    {
        private readonly ApplicationDbContext _context;

        public InformationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Information>> GetAllAsync()
        {
            return await _context.Information
                .AsNoTracking()
                .Include(x => x.Position)
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<Information?> GetByIdAsync(int id)
        {
            var entity = await _context.Information
                .AsNoTracking()
                .Include(x => x.Position)
                .FirstOrDefaultAsync(x => x.InformationId == id);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        {
            var normalized = email.Trim().ToUpperInvariant();

            var query = _context.Information.AsNoTracking()
                .Where(x => x.Email.ToUpper() == normalized);

            if (excludeId.HasValue)
                query = query.Where(x => x.InformationId != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> HasUsersAsync(int id)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.InformationId == id);
        }

        public async Task AddAsync(Information entity)
        {
            var data = entity.ToEntity();
            _context.Information.Add(data);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Information entity)
        {
            var data = await _context.Information.FindAsync(entity.Id);
            if (data == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ cần cập nhật.");

            data.FirstName = entity.FirstName;
            data.LastName = entity.LastName;
            data.Dob = entity.Dob;
            data.Phone = entity.Phone;
            data.Address = entity.Address;
            data.Email = entity.Email;
            data.Gender = entity.Gender;
            data.Avt = entity.Avt;
            data.PositionId = entity.PositionId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var data = await _context.Information.FindAsync(id);
            if (data == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ cần xóa.");

            _context.Information.Remove(data);
            await _context.SaveChangesAsync();
        }
    }
}