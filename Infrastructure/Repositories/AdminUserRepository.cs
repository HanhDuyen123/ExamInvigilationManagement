using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminUserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .Include(x => x.Information)
                .Include(x => x.Faculty)
                .Select(x => x.ToDomain())
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            var entity = await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .Include(x => x.Information)
                .Include(x => x.Faculty)
                .FirstOrDefaultAsync(x => x.UserId == id);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<User>> GetPagedAsync(
            string? keyword,
            int? roleId,
            int? informationId,
            int? facultyId,
            bool? isActive,
            int page,
            int pageSize)
        {
            var query = _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .Include(x => x.Information)
                .Include(x => x.Faculty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lower = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.UserName.ToLower().Contains(lower) ||
                    ((x.Information.LastName + " " + x.Information.FirstName).ToLower().Contains(lower)));
            }

            if (roleId.HasValue)
                query = query.Where(x => x.RoleId == roleId.Value);

            if (informationId.HasValue)
                query = query.Where(x => x.InformationId == informationId.Value);

            if (facultyId.HasValue)
                query = query.Where(x => x.FacultyId == facultyId.Value);

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<User>
            {
                Items = data.Select(x => x.ToDomain()).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> ExistsByUserNameAsync(string userName, int? excludeId = null)
        {
            var normalized = userName.Trim().ToUpperInvariant();

            var query = _context.Users.AsNoTracking()
                .Where(x => x.UserName.ToUpper() == normalized);

            if (excludeId.HasValue)
                query = query.Where(x => x.UserId != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> RoleExistsAsync(byte roleId)
        {
            return await _context.Roles.AsNoTracking().AnyAsync(x => x.RoleId == roleId);
        }

        public async Task<bool> InformationExistsAsync(int informationId)
        {
            return await _context.Information.AsNoTracking().AnyAsync(x => x.InformationId == informationId);
        }

        public async Task<bool> FacultyExistsAsync(int facultyId)
        {
            return await _context.Faculties.AsNoTracking().AnyAsync(x => x.FacultyId == facultyId);
        }

        public async Task<string?> GetRoleNameByIdAsync(byte roleId)
        {
            return await _context.Roles
                .AsNoTracking()
                .Where(x => x.RoleId == roleId)
                .Select(x => x.RoleName)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsInformationInRoleAsync(
            int informationId,
            byte roleId,
            int? excludeUserId = null)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(x => x.InformationId == informationId && x.RoleId == roleId);

            if (excludeUserId.HasValue)
                query = query.Where(x => x.UserId != excludeUserId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> HasDependenciesAsync(int userId)
        {
            return await _context.CourseOfferings.AnyAsync(x => x.UserId == userId)
                   || await _context.LecturerBusySlots.AnyAsync(x => x.UserId == userId)
                   || await _context.ExamInvigilators.AnyAsync(x =>
                        x.AssigneeId == userId ||
                        x.AssignerId == userId ||
                        x.NewAssigneeId == userId)
                   || await _context.ExamScheduleApprovals.AnyAsync(x => x.ApproverId == userId)
                   || await _context.InvigilatorResponses.AnyAsync(x => x.UserId == userId)
                   || await _context.InvigilatorSubstitutions.AnyAsync(x =>
                        x.UserId == userId ||
                        x.SubstituteUserId == userId)
                   || await _context.EmailNotifications.AnyAsync(x => x.UserId == userId)
                   || await _context.Notifications.AnyAsync(x => x.UserId == userId || x.CreatedBy == userId)
                   || await _context.PasswordResetTokens.AnyAsync(x => x.UserId == userId);
        }

        public async Task AddAsync(User entity)
        {
            _context.Users.Add(entity.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User entity)
        {
            var data = await _context.Users.FindAsync(entity.Id);
            if (data == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản cần cập nhật.");

            data.RoleId = entity.RoleId;
            data.InformationId = entity.InformationId;
            data.FacultyId = entity.FacultyId;
            data.UserName = entity.UserName;
            data.IsActive = entity.IsActive;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var data = await _context.Users.FindAsync(id);
            if (data != null)
            {
                _context.Users.Remove(data);
                await _context.SaveChangesAsync();
            }
        }
    }
}