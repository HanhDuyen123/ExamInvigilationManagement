using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Domain.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

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
        username = username?.Trim() ?? string.Empty;

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
    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        request.Username = request.Username?.Trim() ?? string.Empty;
        request.Email = request.Email?.Trim() ?? string.Empty;

        var user = await _repo.GetByUsernameAndEmailAsync(
            request.Username,
            request.Email
        );

        if (user == null)
            return false;

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);

        await _repo.SaveResetTokenAsync(
            user.Id,
            token,
            DateTime.Now.AddMinutes(15)
        );

        var requestContext = _httpContext.HttpContext?.Request;
        var link = requestContext == null
            ? $"/Account/ResetPassword?token={Uri.EscapeDataString(token)}"
            : $"{requestContext.Scheme}://{requestContext.Host}/Account/ResetPassword?token={Uri.EscapeDataString(token)}";

        try
        {
            await _emailService.SendEmailAsync(
                request.Email,
                "Reset Password",
                $"Click vào link: {link}"
            );
            await _emailLogService.LogAsync(user.Id, request.Email, "Sent", null, "ResetPassword");
            return true;
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
            throw new Exception("Mật khẩu xác nhận không khớp.");

        ValidatePasswordPolicy(request.NewPassword);

        var tokenEntity = await _repo.GetValidTokenAsync(request.Token);

        if (tokenEntity == null ||
            tokenEntity.IsUsed ||
            tokenEntity.ExpiredAt < DateTime.Now)
        {
            throw new Exception("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
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
            throw new Exception("Không tìm thấy tài khoản.");

        var isValid = _passwordService.VerifyPassword(
            request.CurrentPassword,
            user.PasswordHash
        );

        if (!isValid)
            throw new Exception("Mật khẩu hiện tại không đúng.");

        if (request.NewPassword != request.ConfirmPassword)
            throw new Exception("Mật khẩu xác nhận không khớp.");

        ValidatePasswordPolicy(request.NewPassword);

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

    private static void ValidatePasswordPolicy(string password)
    {
        if (string.IsNullOrWhiteSpace(password)
            || password.Length < 8
            || !Regex.IsMatch(password, "[A-Za-z]")
            || !Regex.IsMatch(password, "[0-9]")
            || !Regex.IsMatch(password, "[^A-Za-z0-9]"))
        {
            throw new Exception("Mật khẩu phải có ít nhất 8 ký tự, gồm chữ cái, số và ký tự đặc biệt.");
        }
    }
}
