using ExamInvigilationManagement.Application.DTOs.Admin.EmailNotification;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmailNotificationController : Controller
    {
        private readonly IEmailNotificationAdminService _service;

        public EmailNotificationController(IEmailNotificationAdminService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new CrudIndexViewModel
            {
                Title = "Nhật ký email",
                Subtitle = "Tra soát các email hệ thống đã gửi, trạng thái gửi và lỗi phát sinh khi cần kiểm chứng.",
                SearchPartialView = "_EmailNotificationSearch",
                TableClass = "full-width",
                ShowCreateButton = false
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string? keyword, CancellationToken cancellationToken = default)
        {
            var users = await _service.SearchUsersAsync(keyword, cancellationToken);

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            string? keyword,
            int? userId,
            int? facultyId,
            string? status,
            string? type,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 5,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? 5 : pageSize;

            var result = await _service.GetPagedAsync(keyword, userId, facultyId, status, type, fromDate, toDate, page, pageSize, cancellationToken);
            return PartialView("_EmailNotificationTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var item = await _service.GetByIdAsync(id, cancellationToken);

            if (item == null) return NotFound();
            return View(item);
        }
    }
}
