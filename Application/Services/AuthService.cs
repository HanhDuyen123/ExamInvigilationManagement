using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Domain.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IEmailLogService _emailLogService;
    private readonly IHttpContextAccessor _httpContext;

    private const int MAX_FAILED = 5;

    public AuthService(
    IUserRepository repo,
    IPasswordService passwordService,
    IEmailService emailService,
    IEmailLogService emailLogService,
    IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _passwordService = passwordService;
        _emailService = emailService;
        _emailLogService = emailLogService;
        _httpContext = httpContext;
    }

    // ================= LOGIN =================
    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _repo.GetByUsernameAsync(username);

        if (user == null || !user.IsActive)
            return null;

        if (user.IsLocked())
            return null;

        var isValid = _passwordService.VerifyPassword(password, user.PasswordHash);

        if (!isValid)
        {
            user.IncreaseFailedLogin();

            if (user.FailedLoginAttempts >= MAX_FAILED)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(15);
                user.ResetFailedLogin();
            }

            await _repo.UpdateAsync(user);
            return null;
        }

        user.ResetFailedLogin();
        user.LastLogin = DateTime.Now;

        await _repo.UpdateAsync(user);

        return user;
    }

    // ================= FORGOT PASSWORD =================
    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _repo.GetByUsernameAndEmailAsync(
            request.Username,
            request.Email
        );

        if (user == null)
            return;

        var token = Guid.NewGuid().ToString();

        await _repo.SaveResetTokenAsync(
            user.Id,
            token,
            DateTime.Now.AddMinutes(15)
        );

        var link = $"https://localhost:44351/Account/ResetPassword?token={token}";

        try
        {
            await _emailService.SendEmailAsync(
                request.Email,
                "Reset Password",
                $"Click vào link: {link}"
            );
            await _emailLogService.LogAsync(user.Id, request.Email, "Sent", null, "ResetPassword");
        }
        catch (Exception ex)
        {
            await _emailLogService.LogAsync(user.Id, request.Email, "Failed", ex.Message, "ResetPassword");
            throw;
        }
    }

    // ================= RESET PASSWORD =================
    public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new Exception("Password not match");

        var tokenEntity = await _repo.GetValidTokenAsync(request.Token);

        if (tokenEntity == null ||
            tokenEntity.IsUsed ||
            tokenEntity.ExpiredAt < DateTime.Now)
        {
            throw new Exception("Invalid or expired token");
        }

        var hash = _passwordService.HashPassword(request.NewPassword);

        await _repo.UpdatePasswordAsync(tokenEntity.UserId, hash);
        await _repo.MarkTokenAsUsedAsync(request.Token);
    }

    // ================= CHANGE PASSWORD =================
    public async Task ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        var user = await _repo.GetByIdAsync(request.UserId);

        if (user == null)
            throw new Exception("User not found");

        var isValid = _passwordService.VerifyPassword(
            request.CurrentPassword,
            user.PasswordHash
        );

        if (!isValid)
            throw new Exception("Wrong current password");

        if (request.NewPassword != request.ConfirmPassword)
            throw new Exception("Password confirm not match");

        var hash = _passwordService.HashPassword(request.NewPassword);

        await _repo.UpdatePasswordAsync(user.Id, hash);
    }

    public async Task<bool> IsValidTokenAsync(string token)
    {
        var tokenEntity = await _repo.GetValidTokenAsync(token);

        return tokenEntity != null
            && !tokenEntity.IsUsed
            && tokenEntity.ExpiredAt > DateTime.Now;
    }

    public async Task LogoutAsync()
    {
        await _httpContext.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
