using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Infrastructure.Repositories;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExamInvigilationManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDto model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var domainUser = await _authService.LoginAsync(model.UserName, model.Password);

            if (domainUser == null)
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, domainUser.UserName),
                new Claim(ClaimTypes.NameIdentifier, domainUser.Id.ToString()),

                // 🔥 QUAN TRỌNG: PHÂN QUYỀN
                new Claim(ClaimTypes.Role, domainUser.Role?.Name ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
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
        public IActionResult ForgotPassword() => View();
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {

            Console.WriteLine("VÀO CONTROLLER RỒI NHỚ MEOMEO");
            if (!ModelState.IsValid)
                return View(model);

            var requestDto = new ForgotPasswordRequestDto
            {
                Username = model.Username,
                Email = model.Email
            };

            try
            {
                await _authService.ForgotPasswordAsync(requestDto);
                TempData.SetNotification("success", "Link reset mật khẩu đã được gửi nếu thông tin hợp lệ.");
            }
            catch
            {
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
                TempData.SetNotification("success", "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.");
                return RedirectToAction("ResetPasswordSuccess");
            }
            catch (Exception ex)
            {
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
    }
}
