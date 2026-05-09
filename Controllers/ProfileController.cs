using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.ViewModel;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Authentication;
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
    public async Task<IActionResult> Update(UpdateProfileDto dto)
    {
        int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        await _service.UpdateProfileAsync(userId, dto);

        TempData.SetNotification("success", "Cập nhật hồ sơ thành công.");

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

            TempData.SetNotification("success", "Đổi mật khẩu thành công. Vui lòng đăng nhập lại để tiếp tục.");
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
