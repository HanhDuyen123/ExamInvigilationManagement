using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.Import;
using ExamInvigilationManagement.Application.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize]
    public class BulkImportController : Controller
    {
        private readonly IBulkImportService _service;

        public BulkImportController(IBulkImportService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Index(string module)
        {
            if (!CanAccess(module)) return Forbid();

            var model = BuildPage(module);
            return View(model);
        }

        [HttpGet]
        public IActionResult Template(string module)
        {
            if (!CanAccess(module)) return Forbid();

            var bytes = _service.BuildTemplate(module);
            var fileName = $"template-{module}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string module, IFormFile file, CancellationToken cancellationToken)
        {
            if (!CanAccess(module)) return Forbid();

            var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = int.TryParse(userIdText, out var id) ? id : 0;
            var role = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value ?? string.Empty;
            var result = await _service.ImportAsync(module, file, userId, role, cancellationToken);

            var model = BuildPage(module);
            model.Result = result;
            return View("Index", model);
        }

        private ImportPageDto BuildPage(string module) => new()
        {
            Module = module,
            ModuleTitle = _service.GetModuleTitle(module),
            BackUrl = _service.GetBackUrl(module),
            TemplateUrl = Url.Action(nameof(Template), new { module }) ?? "#",
            Columns = _service.GetTemplateColumns(module)
        };

        private bool CanAccess(string module)
        {
            module = (module ?? string.Empty).Trim().ToLowerInvariant();
            if (!_service.SupportedModules.Contains(module)) return false;
            if (module == "exam-invigilator") return User.IsInRole("Thư ký khoa");
            if (module == "lecturer-busy-slot") return User.IsInRole("Admin") || User.IsInRole("Thư ký khoa") || User.IsInRole("Giảng viên");
            if (module == "exam-schedule") return User.IsInRole("Admin");
            return User.IsInRole("Admin");
        }
    }
}
