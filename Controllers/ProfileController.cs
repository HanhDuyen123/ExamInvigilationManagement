using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.ViewModel;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Common.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserService _service;
    private readonly IAuthService _authService;

    public ProfileController(IUserService service, IAuthService authService)
    {
        _service = service;
        _authService = authService;
    }

    public async Task<IActionResult> Index()
    {
        int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var profile = await _service.GetProfileAsync(userId);

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateProfileDto dto, IFormFile? avatarFile)
    {
        int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        try
        {
            if (avatarFile != null && avatarFile.Length > 0)
                dto.Avt = await SaveAvatarAsync(userId, avatarFile);

            await _service.UpdateProfileAsync(userId, dto);
        }
        catch (Exception ex)
        {
            TempData.SetNotification("error", ex.Message);
            return RedirectToAction("Index");
        }

        TempData.SetNotification("success", "Cập nhật hồ sơ thành công.");

        return RedirectToAction("Index");
    }

    private async Task<string> SaveAvatarAsync(int userId, IFormFile file)
    {
        var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ".jpg",
            [".jpeg"] = ".jpg",
            [".png"] = ".png",
            [".webp"] = ".webp"
        };

        var ext = Path.GetExtension(file.FileName);
        if (!allowed.TryGetValue(ext, out var safeExt))
            throw new InvalidOperationException("Ảnh đại diện chỉ hỗ trợ JPG, PNG hoặc WEBP.");

        if (file.Length > 2 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh đại diện không được vượt quá 2MB.");

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(root);

        var fileName = $"user-{userId}-{Guid.NewGuid():N}{safeExt}";
        var path = Path.Combine(root, fileName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return $"/uploads/avatars/{fileName}";
    }

    [HttpGet]
    [RequireRecentAuthentication]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRecentAuthentication]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _authService.ChangePasswordAsync(new ChangePasswordRequestDto
            {
                UserId = userId,
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmPassword = model.ConfirmPassword
            });

            TempData.SetNotification("success", "Đổi mật khẩu thành công. Vui lòng đăng nhập lại bằng mật khẩu mới.");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        catch
        {
            ModelState.AddModelError("", "Mật khẩu hiện tại không đúng");
            TempData.SetNotification("error", "Mật khẩu hiện tại không đúng.");
            return View(model);
        }
    }
}
