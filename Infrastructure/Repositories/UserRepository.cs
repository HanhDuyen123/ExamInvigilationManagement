using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Data.Entities;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Domain.Entities.User?> GetByUsernameAndEmailAsync(string username, string email) 
        { 
            var user = await _context.Users.Include(x => x.Information)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(u => u.UserName == username && u.Information.Email == email); 
            return user?.ToDomain(); 
        }

        public async Task<Domain.Entities.User?> GetByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(x => x.Information)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return user?.ToDomain();
        }
        public async Task<Domain.Entities.User?> GetByUsernameAsync(string username)
        {
            var user = await _context.Users
                .Include(x => x.Information)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserName == username);

            return user?.ToDomain();
        }
        public async Task<Domain.Entities.User?> GetProfileByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(x => x.Information)
                    .ThenInclude(i => i.Position)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return user?.ToDomain();
        }

        public async Task UpdateAsync(Domain.Entities.User user)
        {
            var entity = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (entity == null) return;

            entity.FailedLoginAttempts = user.FailedLoginAttempts;
            entity.LockoutEnd = user.LockoutEnd;
            entity.LastLogin = user.LastLogin;
            entity.IsActive = user.IsActive;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateProfileAsync(Domain.Entities.User domain)
        {
            var entity = await _context.Users
                .Include(x => x.Information)
                .FirstOrDefaultAsync(x => x.UserId == domain.Id);

            if (entity == null) return;

            entity.Information.FirstName = domain.Information.FirstName;
            entity.Information.LastName = domain.Information.LastName;
            entity.Information.Phone = domain.Information.Phone;
            entity.Information.Address = domain.Information.Address;

            await _context.SaveChangesAsync();
        }   

        public async Task SaveResetTokenAsync(int userId, string token, DateTime expiredAt)
        {
            var entity = new PasswordResetToken
            {
                UserId = userId,
                Token = token,
                ExpiredAt = expiredAt,
                IsUsed = false
            };

            _context.PasswordResetTokens.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<Domain.Entities.PasswordResetToken?> GetValidTokenAsync(string token)
        {
            var entity = await _context.PasswordResetTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Token == token &&
                    !x.IsUsed &&
                    x.ExpiredAt > DateTime.Now);

            return entity?.ToDomain();
        }

        public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            var user = await _context.Users.FindAsync(userId);
            user.PasswordHash = newPasswordHash;

            await _context.SaveChangesAsync();
        }

        public async Task MarkTokenAsUsedAsync(string token)
        {
            var entity = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(x => x.Token == token);

            if (entity != null)
            {
                entity.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
