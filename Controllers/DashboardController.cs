using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.Dashboard;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Constants;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize] // Bắt buộc login mới vào dashboard
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var roleName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var metrics = await _dashboardService.GetMetricsAsync(roleName, userId, cancellationToken);

            var model = new DashboardIndexViewModel { RoleName = roleName };

            if (metrics.Admin != null)
                AddAdminItems(model, metrics.Admin);
            else if (metrics.Secretary != null)
                AddSecretaryItems(model, metrics.Secretary);
            else if (metrics.Dean != null)
                AddDeanItems(model, metrics.Dean);
            else if (metrics.Lecturer != null)
                AddLecturerItems(model, metrics.Lecturer);

            ViewBag.RoleName = roleName;
            return View(model);
        }

        private void AddAdminItems(DashboardIndexViewModel model, DashboardAdminMetricsDto metrics)
        {
            model.WorkItems.Add(new() { Title = "Lịch thiếu giám thị", Description = "Cần kiểm tra toàn hệ thống.", Count = metrics.MissingInvigilatorSchedules, Url = Url.Action("Index", "ExamSchedule", new { status = ExamScheduleStatuses.MissingInvigilator }) ?? "#", Icon = "bi-person-exclamation", Tone = "warning" });
            model.WorkItems.Add(new() { Title = "Outbox lỗi", Description = "Sự kiện nền cần xử lý lại.", Count = metrics.FailedOutboxMessages, Url = Url.Action("Index", "Outbox", new { area = "Admin", status = "Failed" }) ?? "#", Icon = "bi-exclamation-octagon", Tone = "danger" });
            model.WorkItems.Add(new() { Title = "Người dùng đang hoạt động", Description = "Quy mô tài khoản hiện tại.", Count = metrics.ActiveUsers, Url = Url.Action("Index", "User", new { area = "Admin" }) ?? "#", Icon = "bi-people", Tone = "primary" });
        }

        private void AddSecretaryItems(DashboardIndexViewModel model, DashboardSecretaryMetricsDto metrics)
        {
            model.WorkItems.Add(new() { Title = "Cần phân công", Description = "Lịch đang chờ hoặc thiếu giám thị.", Count = metrics.WaitingAssignSchedules, Url = Url.Action("Index", "ExamSchedule", new { status = ExamScheduleStatuses.WaitingAssign }) ?? "#", Icon = "bi-person-plus", Tone = metrics.OverdueAssignSchedules > 0 ? "danger" : "warning", BadgeText = metrics.OverdueAssignSchedules > 0 ? $"{metrics.OverdueAssignSchedules} lịch sát ngày thi" : null });
            model.WorkItems.Add(new() { Title = "Chờ gửi duyệt", Description = "Lịch đủ điều kiện nhưng chưa gửi duyệt.", Count = metrics.PendingSendApprovalSchedules, Url = Url.Action("Index", "ExamSchedule", new { status = ExamScheduleStatuses.PendingApproval }) ?? "#", Icon = "bi-send", Tone = metrics.OverdueApprovalSchedules > 0 ? "warning" : "primary", BadgeText = metrics.OverdueApprovalSchedules > 0 ? $"{metrics.OverdueApprovalSchedules} lịch sát ngày thi" : null });
            model.WorkItems.Add(new() { Title = "Đề xuất thay thế", Description = "Giảng viên từ chối và cần thư ký xử lý.", Count = metrics.ProposedSubstitutions, Url = Url.Action("Index", "InvigilatorSubstitution", new { area = "Secretary", Status = InvigilatorSubstitutionStatuses.Proposed }) ?? "#", Icon = "bi-arrow-left-right", Tone = "danger" });
        }

        private void AddDeanItems(DashboardIndexViewModel model, DashboardDeanMetricsDto metrics)
        {
            model.WorkItems.Add(new() { Title = "Lịch chờ duyệt", Description = "Cần trưởng khoa duyệt hoặc từ chối.", Count = metrics.PendingApprovals, Url = Url.Action("Index", "ExamScheduleApproval", new { area = "Secretary", status = ExamScheduleStatuses.PendingApproval }) ?? "#", Icon = "bi-check2-circle", Tone = metrics.OverdueApprovals > 0 ? "warning" : "primary", BadgeText = metrics.OverdueApprovals > 0 ? $"{metrics.OverdueApprovals} lịch sát ngày thi" : null });
        }

        private void AddLecturerItems(DashboardIndexViewModel model, DashboardLecturerMetricsDto metrics)
        {
            model.WorkItems.Add(new() { Title = "Chưa phản hồi", Description = "Lịch coi thi cần xác nhận hoặc từ chối.", Count = metrics.PendingResponses, Url = Url.Action("Index", "InvigilatorResponse", new { area = "Lecturer", status = "Chưa phản hồi" }) ?? "#", Icon = "bi-check2-square", Tone = metrics.OverdueResponses > 0 ? "danger" : "warning", BadgeText = metrics.OverdueResponses > 0 ? $"{metrics.OverdueResponses} lịch sát ngày thi" : null });
            model.WorkItems.Add(new() { Title = "Cần đề xuất thay thế", Description = "Lịch đã từ chối nhưng chưa có đề xuất.", Count = metrics.RejectedWithoutSubstitution, Url = Url.Action("Index", "InvigilatorResponse", new { area = "Lecturer", status = InvigilatorResponseStatuses.Rejected }) ?? "#", Icon = "bi-arrow-left-right", Tone = "danger" });
        }
    }
}
