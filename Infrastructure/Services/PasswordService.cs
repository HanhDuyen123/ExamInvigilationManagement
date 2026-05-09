using ExamInvigilationManagement.Application.Interfaces.Common;
using Microsoft.AspNetCore.Identity;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Infrastructure.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly IPasswordHasher<User> _hasher;

        public PasswordService(IPasswordHasher<User> hasher)
        {
            _hasher = hasher;
        }

        public string HashPassword(string password)
        {
            return _hasher.HashPassword(null!, password);
        }

        public bool VerifyPassword(string password, string hash)
        {
            return _hasher.VerifyHashedPassword(null!, hash, password)
                == PasswordVerificationResult.Success;
        }
    }
}
