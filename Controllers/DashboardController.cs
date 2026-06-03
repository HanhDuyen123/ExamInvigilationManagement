using System.Security.Claims;
using ExamInvigilationManagement.Common.Constants;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize] // Bắt buộc login mới vào dashboard
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var roleName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var facultyId = userId > 0
                ? await _db.Users.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.FacultyId).FirstOrDefaultAsync(cancellationToken)
                : null;

            var model = new DashboardIndexViewModel { RoleName = roleName };

            if (string.Equals(roleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
                await AddAdminItemsAsync(model, cancellationToken);
            else if (string.Equals(roleName, RoleNames.Secretary, StringComparison.OrdinalIgnoreCase) && facultyId.HasValue)
                await AddSecretaryItemsAsync(model, facultyId.Value, cancellationToken);
            else if (string.Equals(roleName, RoleNames.Dean, StringComparison.OrdinalIgnoreCase) && userId > 0)
                await AddDeanItemsAsync(model, userId, cancellationToken);
            else if (string.Equals(roleName, RoleNames.Lecturer, StringComparison.OrdinalIgnoreCase) && userId > 0)
                await AddLecturerItemsAsync(model, userId, cancellationToken);

            ViewBag.RoleName = roleName;
            return View(model);
        }

        private async Task AddAdminItemsAsync(DashboardIndexViewModel model, CancellationToken cancellationToken)
        {
            model.WorkItems.Add(new() { Title = "Lịch thiếu giám thị", Description = "Cần kiểm tra toàn hệ thống.", Count = await _db.ExamSchedules.CountAsync(x => x.Status == ExamScheduleStatuses.MissingInvigilator, cancellationToken), Url = Url.Action("Index", "ExamSchedule", new { status = ExamScheduleStatuses.MissingInvigilator }) ?? "#", Icon = "bi-person-exclamation", Tone = "warning" });
            model.WorkItems.Add(new() { Title = "Outbox lỗi", Description = "Sự kiện nền cần xử lý lại.", Count = await _db.OutboxMessages.CountAsync(x => x.Status == "Failed", cancellationToken), Url = Url.Action("Index", "Outbox", new { area = "Admin", status = "Failed" }) ?? "#", Icon = "bi-exclamation-octagon", Tone = "danger" });
            model.WorkItems.Add(new() { Title = "Người dùng đang hoạt động", Description = "Quy mô tài khoản hiện tại.", Count = await _db.Users.CountAsync(x => x.IsActive, cancellationToken), Url = Url.Action("Index", "User", new { area = "Admin" }) ?? "#", Icon = "bi-people", Tone = "primary" });
        }

        private async Task AddSecretaryItemsAsync(DashboardIndexViewModel model, int facultyId, CancellationToken cancellationToken)
        {
            var schedules = _db.ExamSchedules.Where(x => x.Offering.Subject.FacultyId == facultyId);
            var today = DateTime.Today;
            var overdueAssign = await schedules.CountAsync(x => (x.Status == ExamScheduleStatuses.WaitingAssign || x.Status == ExamScheduleStatuses.MissingInvigilator) && x.ExamDate.Date <= today.AddDays(3), cancellationToken);
            var overdueApproval = await schedules.CountAsync(x => x.Status == ExamScheduleStatuses.PendingApproval && !x.ExamScheduleApprovals.Any(a => a.Status == ExamScheduleStatuses.PendingApproval) && x.ExamDate.Date <= today.AddDays(3), cancellationToken);
            model.WorkItems.Add(new() { Title = "Cần phân công", Description = "Lịch đang chờ hoặc thiếu giám thị.", Count = await schedules.CountAsync(x => x.Status == ExamScheduleStatuses.WaitingAssign || x.Status == ExamScheduleStatuses.MissingInvigilator, cancellationToken), Url = Url.Action("Index", "ExamSchedule", new { status = ExamScheduleStatuses.WaitingAssign }) ?? "#", Icon = "bi-person-plus", Tone = overdueAssign > 0 ? "danger" : "warning", BadgeText = overdueAssign > 0 ? $"{overdueAssign} lịch sát ngày thi" : null });
            model.WorkItems.Add(new() { Title = "Chờ gửi duyệt", Description = "Lịch đủ điều kiện nhưng chưa gửi duyệt.", Count = await schedules.CountAsync(x => x.Status == ExamScheduleStatuses.PendingApproval && !x.ExamScheduleApprovals.Any(a => a.Status == ExamScheduleStatuses.PendingApproval), cancellationToken), Url = Url.Action("Index", "ExamSchedule", new { status = ExamScheduleStatuses.PendingApproval }) ?? "#", Icon = "bi-send", Tone = overdueApproval > 0 ? "warning" : "primary", BadgeText = overdueApproval > 0 ? $"{overdueApproval} lịch sát ngày thi" : null });
            model.WorkItems.Add(new() { Title = "Đề xuất thay thế", Description = "Giảng viên từ chối và cần thư ký xử lý.", Count = await _db.InvigilatorSubstitutions.CountAsync(x => x.Status == InvigilatorSubstitutionStatuses.Proposed && x.ExamInvigilator.ExamSchedule.Offering.Subject.FacultyId == facultyId, cancellationToken), Url = Url.Action("Index", "InvigilatorSubstitution", new { area = "Secretary", Status = InvigilatorSubstitutionStatuses.Proposed }) ?? "#", Icon = "bi-arrow-left-right", Tone = "danger" });
        }

        private async Task AddDeanItemsAsync(DashboardIndexViewModel model, int userId, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var pendingApprovals = _db.ExamScheduleApprovals
                .Where(x =>
                    x.ApproverId == userId &&
                    x.Status == ExamScheduleStatuses.PendingApproval &&
                    x.ExamSchedule.Status == ExamScheduleStatuses.PendingApproval);
            var overdue = await pendingApprovals.CountAsync(x => x.ExamSchedule.ExamDate.Date <= today.AddDays(3), cancellationToken);
            model.WorkItems.Add(new() { Title = "Lịch chờ duyệt", Description = "Cần trưởng khoa duyệt hoặc từ chối.", Count = await pendingApprovals.CountAsync(cancellationToken), Url = Url.Action("Index", "ExamScheduleApproval", new { area = "Secretary", status = ExamScheduleStatuses.PendingApproval }) ?? "#", Icon = "bi-check2-circle", Tone = overdue > 0 ? "warning" : "primary", BadgeText = overdue > 0 ? $"{overdue} lịch sát ngày thi" : null });
        }

        private async Task AddLecturerItemsAsync(DashboardIndexViewModel model, int userId, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var currentInformationId = await _db.Users.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (int?)x.InformationId)
                .FirstOrDefaultAsync(cancellationToken);

            var pendingQuery = _db.ExamInvigilators.Where(x =>
                (x.AssigneeId == userId || (currentInformationId.HasValue && x.Assignee.InformationId == currentInformationId.Value)) &&
                x.ExamSchedule.Status == ExamScheduleStatuses.Approved &&
                x.ConfirmationSentAt.HasValue &&
                !x.InvigilatorResponses.Any(r => r.UserId == userId || (currentInformationId.HasValue && r.User.InformationId == currentInformationId.Value)));

            var overdue = await pendingQuery.CountAsync(x => x.ExamSchedule.ExamDate.Date <= today.AddDays(3), cancellationToken);
            model.WorkItems.Add(new() { Title = "Chưa phản hồi", Description = "Lịch coi thi cần xác nhận hoặc từ chối.", Count = await pendingQuery.CountAsync(cancellationToken), Url = Url.Action("Index", "InvigilatorResponse", new { area = "Lecturer", status = "Chưa phản hồi" }) ?? "#", Icon = "bi-check2-square", Tone = overdue > 0 ? "danger" : "warning", BadgeText = overdue > 0 ? $"{overdue} lịch sát ngày thi" : null });
            model.WorkItems.Add(new() { Title = "Cần đề xuất thay thế", Description = "Lịch đã từ chối nhưng chưa có đề xuất.", Count = await _db.ExamInvigilators.CountAsync(x => x.AssigneeId == userId && x.InvigilatorResponses.Any(r => r.UserId == userId && r.Status == InvigilatorResponseStatuses.Rejected) && !x.InvigilatorSubstitutions.Any(s => s.UserId == userId), cancellationToken), Url = Url.Action("Index", "InvigilatorResponse", new { area = "Lecturer", status = InvigilatorResponseStatuses.Rejected }) ?? "#", Icon = "bi-arrow-left-right", Tone = "danger" });
        }
    }
}
