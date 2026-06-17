using ExamInvigilationManagement.Application.DTOs.Admin.Audit;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AuditLogController : Controller
{
    private readonly IAuditLogService _service;

    public AuditLogController(IAuditLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new CrudIndexViewModel
        {
            Title = "Lịch sử hệ thống",
            Subtitle = "Tra cứu thay đổi dữ liệu và các thao tác nghiệp vụ quan trọng.",
            SearchPartialView = "_AuditLogSearch",
            TableClass = "full-width",
            ShowCreateButton = false
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        string? keyword,
        string? entityName,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var result = await _service.GetPagedAsync(keyword, entityName, action, fromDate, toDate, page, pageSize, cancellationToken);
        return PartialView("_AuditLogTable", result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);

        return item == null ? NotFound() : View(item);
    }
}
