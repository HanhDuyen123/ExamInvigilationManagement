using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.DTOs.Import;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.ViewModel;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserService _service;
    private readonly IAuthService _authService;
    private readonly IAvatarStorageService _avatarStorageService;
    private readonly IRequestContextService _requestContextService;

    public ProfileController(IUserService service, IAuthService authService, IAvatarStorageService avatarStorageService, IRequestContextService requestContextService)
    {
        _service = service;
        _authService = authService;
        _avatarStorageService = avatarStorageService;
        _requestContextService = requestContextService;
    }

    public async Task<IActionResult> Index()
    {
        int userId = GetCurrentUserId();

        var profile = await _service.GetProfileAsync(userId);

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateProfileDto dto, IFormFile? avatarFile)
    {
        int userId = GetCurrentUserId();

        try
        {
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var importFile = new ImportFileDto
                {
                    FileName = avatarFile.FileName,
                    Length = avatarFile.Length,
                    OpenReadStream = avatarFile.OpenReadStream
                };
                dto.Avt = await _avatarStorageService.SaveAvatarAsync(userId, importFile, cancellationToken: HttpContext.RequestAborted);
            }

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
        int userId = GetCurrentUserId();

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
            await _requestContextService.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
        catch
        {
            ModelState.AddModelError("", "Mật khẩu hiện tại không đúng");
            TempData.SetNotification("error", "Mật khẩu hiện tại không đúng.");
            return View(model);
        }
    }

    private int GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : 0;
    }
}
