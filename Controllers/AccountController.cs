using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Common.Security;
using ExamInvigilationManagement.Infrastructure.Repositories;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExamInvigilationManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = GetSafeReturnUrl(returnUrl);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> Login(LoginRequestDto model, string? returnUrl = null)
        {
            returnUrl = GetSafeReturnUrl(returnUrl);
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            model.UserName = model.UserName?.Trim() ?? string.Empty;

            var domainUser = await _authService.LoginAsync(model.UserName, model.Password);

            if (domainUser == null)
            {
                _logger.LogWarning("Login failed. UserName={UserName}, RemoteIp={RemoteIp}", model.UserName, HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, domainUser.UserName),
                new Claim(ClaimTypes.NameIdentifier, domainUser.Id.ToString()),
                new Claim(AuthSessionClaimTypes.PasswordVersion, AuthSessionVersion.FromPasswordHash(domainUser.Id, domainUser.PasswordHash)),
                new Claim(AuthSessionClaimTypes.RecentAuthenticationUtc, DateTimeOffset.UtcNow.ToString("O")),

                new Claim(ClaimTypes.Role, domainUser.Role?.Name ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var authProperties = GetAuthProperties(domainUser.Role?.Name);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            _logger.LogInformation("Login succeeded. UserId={UserId}, Role={Role}, RemoteIp={RemoteIp}", domainUser.Id, domainUser.Role?.Name, HttpContext.Connection.RemoteIpAddress?.ToString());

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Denied(string? returnUrl = null)
        {
            TempData.SetNotification("error", "Bạn không có quyền truy cập vào trang này.");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }


        [HttpGet]
        [Authorize]
        public IActionResult Reauthenticate(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = GetSafeReturnUrl(returnUrl) ?? Url.Action("Index", "Dashboard");
            return View(new LoginRequestDto { UserName = User.Identity?.Name ?? string.Empty });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> Reauthenticate(LoginRequestDto model, string? returnUrl = null)
        {
            returnUrl = GetSafeReturnUrl(returnUrl) ?? Url.Action("Index", "Dashboard");
            ViewBag.ReturnUrl = returnUrl;

            var currentUserName = User.Identity?.Name ?? string.Empty;
            model.UserName = currentUserName;

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Vui lòng nhập mật khẩu.");
                return View(model);
            }

            var domainUser = await _authService.LoginAsync(currentUserName, model.Password);
            if (domainUser == null)
            {
                _logger.LogWarning("Step-up re-authentication failed. UserName={UserName}, RemoteIp={RemoteIp}", currentUserName, HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError("", "Mật khẩu xác thực không đúng hoặc tài khoản không còn hợp lệ.");
                return View(model);
            }

            await SignInUserAsync(domainUser);
            _logger.LogInformation("Step-up re-authentication succeeded. UserId={UserId}, RemoteIp={RemoteIp}", domainUser.Id, HttpContext.Connection.RemoteIpAddress?.ToString());

            return Redirect(returnUrl!);
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {

            if (!ModelState.IsValid)
                return View(model);

            model.Username = model.Username?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;

            var requestDto = new ForgotPasswordRequestDto
            {
                Username = model.Username,
                Email = model.Email
            };

            try
            {
                var isValid = await _authService.ForgotPasswordAsync(requestDto);
                _logger.LogInformation("Forgot password requested. UserName={UserName}, Email={Email}, IsValid={IsValid}, RemoteIp={RemoteIp}", model.Username, model.Email, isValid, HttpContext.Connection.RemoteIpAddress?.ToString());

                if (!isValid)
                {
                    TempData.SetNotification("error", "Tên đăng nhập hoặc email không đúng. Vui lòng kiểm tra lại thông tin.");
                    return View(model);
                }

                TempData.SetNotification("success", "Thông tin hợp lệ. Liên kết đặt lại mật khẩu đã được gửi đến email của bạn.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Forgot password failed. UserName={UserName}, Email={Email}, RemoteIp={RemoteIp}", model.Username, model.Email, HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData.SetNotification("error", "Có lỗi xảy ra, vui lòng thử lại.");
            }

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return View("InvalidToken");

            if (!await _authService.IsValidTokenAsync(token))
            {
                return View("InvalidToken");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var requestDto = new ResetPasswordRequestDto
            {
                Token = model.Token,
                NewPassword = model.NewPassword,
                ConfirmPassword = model.ConfirmPassword
            };

            try
            {
                await _authService.ResetPasswordAsync(requestDto);
                _logger.LogInformation("Password reset completed. RemoteIp={RemoteIp}", HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData.SetNotification("success", "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.");
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password reset failed. RemoteIp={RemoteIp}", HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError("", ex.Message);
                TempData.SetNotification("error", ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPasswordSuccess()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private string? GetSafeReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;
        }

        private async Task SignInUserAsync(ExamInvigilationManagement.Domain.Entities.User domainUser)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, domainUser.UserName),
                new Claim(ClaimTypes.NameIdentifier, domainUser.Id.ToString()),
                new Claim(AuthSessionClaimTypes.PasswordVersion, AuthSessionVersion.FromPasswordHash(domainUser.Id, domainUser.PasswordHash)),
                new Claim(AuthSessionClaimTypes.RecentAuthenticationUtc, DateTimeOffset.UtcNow.ToString("O")),
                new Claim(ClaimTypes.Role, domainUser.Role?.Name ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                GetAuthProperties(domainUser.Role?.Name));
        }

        private static AuthenticationProperties GetAuthProperties(string? roleName)
        {
            var isPrivileged = roleName is "Admin" or "Thư ký khoa" or "Trưởng khoa";

            return new AuthenticationProperties
            {
                IsPersistent = !isPrivileged,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(isPrivileged ? 45 : 120),
                AllowRefresh = true
            };
        }
    }
}
