using ExamInvigilationManagement.Application.DTOs.Admin.Outbox;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OutboxController : Controller
{
    private readonly IOutboxService _service;

    public OutboxController(IOutboxService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new CrudIndexViewModel
        {
            Title = "Theo dõi Outbox",
            Subtitle = "Kiểm tra sự kiện nền, lỗi retry và các message cần xử lý lại.",
            SearchPartialView = "_OutboxSearch",
            TableClass = "full-width",
            ShowCreateButton = false
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        string? keyword,
        string? status,
        string? type,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var result = await _service.GetPagedAsync(keyword, status, type, fromDate, toDate, page, pageSize, cancellationToken);
        return PartialView("_OutboxTable", result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);

        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(long id, CancellationToken cancellationToken = default)
    {
        var updated = await _service.RetryAsync(id, cancellationToken);
        if (!updated) return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }
}
