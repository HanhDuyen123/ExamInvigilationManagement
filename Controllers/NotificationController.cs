using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _service;
        private readonly IAdminUserService _userService;

        public NotificationController(INotificationService service, IAdminUserService userService)
        {
            _service = service;
            _userService = userService;
        }

        public async Task<IActionResult> Index(
            [FromQuery] NotificationSearchDto search,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var isAdmin = User.IsInRole("Admin");
            var paged = await _service.GetPagedAsync(userId.Value, isAdmin, search, page, pageSize, cancellationToken);
            var unreadCount = await _service.GetUnreadCountAsync(userId.Value, cancellationToken);

            return View(new NotificationIndexPageDto
            {
                Search = search,
                PagedItems = paged,
                TotalCount = paged.TotalCount,
                UnreadCount = unreadCount,
                Page = page,
                PageSize = pageSize,
                IsAdmin = isAdmin
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] NotificationSearchDto search,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var paged = await _service.GetPagedAsync(userId.Value, User.IsInRole("Admin"), search, page, pageSize, cancellationToken);
            return PartialView("_NotificationTable", paged);
        }

        [HttpGet]
        public async Task<IActionResult> Open(int id, CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var item = await _service.GetByIdAsync(id, userId.Value, cancellationToken);
            if (item is null) return NotFound();

            await _service.MarkAsReadAsync(id, userId.Value, cancellationToken);
            return Redirect(NotificationRouteHelper.ResolveUrl(item.Type, item.RelatedId));
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string? keyword, CancellationToken cancellationToken = default)
        {
            var paged = await _userService.GetPagedAsync(null, null, null, null, true, 1, 1000);
            var users = paged.Items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                users = users.Where(x =>
                    (x.FullName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            return Json(users.Take(20).Select(x => new
            {
                id = x.Id,
                name = string.IsNullOrWhiteSpace(x.FullName)
                    ? x.UserName
                    : $"{x.FullName} ({x.UserName})"
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Recent(CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var items = await _service.GetRecentAsync(userId.Value, 5, cancellationToken);
            return PartialView("_NotificationDropdownItems", items);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var count = await _service.GetUnreadCountAsync(userId.Value, cancellationToken);
            return Json(new { unreadCount = count });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            return await Open(id, cancellationToken);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead([FromBody] NotificationMarkReadRequestDto request, CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var ok = await _service.MarkAsReadAsync(request.Id, userId.Value, cancellationToken);
            return Json(new { success = ok });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var count = await _service.MarkAllAsReadAsync(userId.Value, cancellationToken);
            return Json(new { success = true, updated = count });
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
