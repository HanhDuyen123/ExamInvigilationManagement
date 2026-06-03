using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Domain.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IEmailLogService _emailLogService;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IConfiguration _configuration;

    private const int MAX_FAILED = 5;

    public AuthService(
    IUserRepository repo,
    IPasswordService passwordService,
    IEmailService emailService,
    IEmailLogService emailLogService,
    IHttpContextAccessor httpContext,
    IConfiguration configuration)
    {
        _repo = repo;
        _passwordService = passwordService;
        _emailService = emailService;
        _emailLogService = emailLogService;
        _httpContext = httpContext;
        _configuration = configuration;
    }

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

        var link = BuildAbsoluteUrl($"/Account/ResetPassword?token={Uri.EscapeDataString(token)}");

        try
        {
            await _emailService.SendEmailAsync(
                request.Email,
                "Đặt lại mật khẩu hệ thống quản lý coi thi",
                BuildResetPasswordEmail(user.UserName, link)
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

    private string BuildAbsoluteUrl(string path)
    {
        var baseUrl = _configuration["App:BaseUrl"]?.Trim().TrimEnd('/');
        var requestContext = _httpContext.HttpContext?.Request;
        if (string.IsNullOrWhiteSpace(baseUrl) && requestContext != null)
        {
            baseUrl = $"{requestContext.Scheme}://{requestContext.Host}".TrimEnd('/');
        }

        return string.IsNullOrWhiteSpace(baseUrl)
            ? path
            : $"{baseUrl}/{path.TrimStart('/')}";
    }

    private static string BuildResetPasswordEmail(string userName, string link)
    {
        var safeUserName = WebUtility.HtmlEncode(userName);
        var safeLink = WebUtility.HtmlEncode(link);
        var requestedAt = WebUtility.HtmlEncode(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

        return $@"
<!doctype html>
<html lang='vi'>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1'>
  <title>Đặt lại mật khẩu</title>
</head>
<body style='margin:0;background:#f1f5f9;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;'>
  <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background:#f1f5f9;padding:28px 12px;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width:620px;background:#ffffff;border-radius:22px;overflow:hidden;border:1px solid #e2e8f0;box-shadow:0 20px 48px rgba(15,23,42,.10);'>
          <tr>
            <td style='padding:28px 30px;background:linear-gradient(135deg,#0f766e,#2563eb);color:#ffffff;'>
              <div style='font-size:13px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;opacity:.86;'>Exam Invigilation Management</div>
              <h1 style='margin:10px 0 0;font-size:24px;line-height:1.25;'>Yêu cầu đặt lại mật khẩu</h1>
            </td>
          </tr>
          <tr>
            <td style='padding:30px;'>
              <p style='margin:0 0 14px;font-size:16px;line-height:1.6;'>Xin chào <strong>{safeUserName}</strong>,</p>
              <p style='margin:0 0 22px;font-size:15px;line-height:1.7;color:#334155;'>Hệ thống nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Vui lòng nhấn nút bên dưới để tạo mật khẩu mới.</p>
              <p style='margin:0 0 24px;text-align:center;'>
                <a href='{safeLink}' style='display:inline-block;background:#0f766e;color:#ffffff;text-decoration:none;font-weight:700;padding:13px 22px;border-radius:14px;'>Đặt lại mật khẩu</a>
              </p>
              <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:16px;padding:14px 16px;margin-bottom:22px;'>
                <div style='font-size:13px;color:#64748b;margin-bottom:4px;'>Thời gian yêu cầu</div>
                <div style='font-size:15px;font-weight:700;color:#0f172a;'>{requestedAt}</div>
                <div style='font-size:13px;color:#64748b;margin-top:10px;'>Liên kết có hiệu lực trong 15 phút.</div>
              </div>
              <p style='margin:0 0 12px;font-size:14px;line-height:1.6;color:#475569;'>Nếu nút không hoạt động, hãy mở liên kết sau:</p>
              <p style='margin:0 0 22px;font-size:13px;line-height:1.6;word-break:break-all;color:#2563eb;'>{safeLink}</p>
              <p style='margin:0;font-size:14px;line-height:1.6;color:#64748b;'>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Mật khẩu hiện tại của bạn sẽ không thay đổi.</p>
            </td>
          </tr>
          <tr>
            <td style='padding:18px 30px;background:#f8fafc;border-top:1px solid #e2e8f0;color:#64748b;font-size:13px;line-height:1.6;'>
              Email này được gửi tự động từ hệ thống quản lý coi thi. Vui lòng không trả lời trực tiếp email này.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
