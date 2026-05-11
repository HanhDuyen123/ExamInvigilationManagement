using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string username, string password);
        Task LogoutAsync();

        Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<bool> IsValidTokenAsync(string token);
        Task ChangePasswordAsync(ChangePasswordRequestDto request);

    }
}
