using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.Statistics;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize(Roles = "Admin,Giảng viên,Thư ký khoa,Trưởng khoa")]
    public class StatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] StatisticsFilterDto filter, CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var model = await _statisticsService.GetDashboardAsync(userId.Value, GetCurrentRole(), filter, cancellationToken);
                return View(model);
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View(new StatisticsDashboardDto { RoleName = GetCurrentRole(), Filter = filter });
            }
            catch
            {
                TempData.SetNotification("error", "Không thể tải dữ liệu thống kê lúc này. Vui lòng kiểm tra lại bộ lọc hoặc thử lại sau.");
                return View(new StatisticsDashboardDto { RoleName = GetCurrentRole(), Filter = filter });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Preview([FromQuery] StatisticsFilterDto filter, CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var model = await _statisticsService.GetDashboardAsync(userId.Value, GetCurrentRole(), filter, cancellationToken);
                return View(model);
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction(nameof(Index), filter);
            }
            catch
            {
                TempData.SetNotification("error", "Không thể tạo bản xem trước báo cáo. Vui lòng thử lại sau.");
                return RedirectToAction(nameof(Index), filter);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] StatisticsFilterDto filter, string format = "pdf", CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var model = await _statisticsService.GetDashboardAsync(userId.Value, GetCurrentRole(), filter, cancellationToken);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
                if (format.Equals("csv", StringComparison.OrdinalIgnoreCase) || format.Equals("excel", StringComparison.OrdinalIgnoreCase))
                    return File(_statisticsService.ExportCsv(model), "text/csv; charset=utf-8", $"thong-ke-coi-thi-{timestamp}.csv");

                return File(_statisticsService.ExportPdf(model), "application/pdf", $"thong-ke-coi-thi-{timestamp}.pdf");
            }
            catch (InvalidOperationException ex)
            {
                TempData.SetNotification("error", ex.Message);
                return RedirectToAction(nameof(Index), filter);
            }
            catch
            {
                TempData.SetNotification("error", "Không thể export thống kê lúc này. Vui lòng thử lại sau.");
                return RedirectToAction(nameof(Index), filter);
            }
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }

        private string GetCurrentRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        }
    }
}
