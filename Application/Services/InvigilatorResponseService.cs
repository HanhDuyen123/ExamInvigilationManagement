using System.Text.Encodings.Web;
using ExamInvigilationManagement.Application.DTOs.InvigilatorResponse;
using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Helpers;

namespace ExamInvigilationManagement.Application.Services
{
    public class InvigilatorResponseService : IInvigilatorResponseService
    {
        private const string Confirmed = "Xác nhận";
        private const string Rejected = "Từ chối";
        private static readonly TimeSpan ResponseWindow = TimeSpan.FromHours(48);

        private readonly IInvigilatorResponseRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IEmailLogService _emailLogService;

        public InvigilatorResponseService(
            IInvigilatorResponseRepository repository,
            INotificationService notificationService,
            IEmailService emailService,
            IEmailLogService emailLogService)
        {
            _repository = repository;
            _notificationService = notificationService;
            _emailService = emailService;
            _emailLogService = emailLogService;
        }

        public async Task<PagedResult<InvigilatorAssignmentItemDto>> GetAssignmentsAsync(
            int userId,
            InvigilatorAssignmentSearchDto search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            await _repository.AutoConfirmExpiredAsync(ResponseWindow, cancellationToken);
            return await _repository.GetAssignmentsAsync(userId, search, page, pageSize, cancellationToken);
        }

        public async Task<InvigilatorResponseResultDto> SubmitAsync(
            int userId,
            InvigilatorResponseSubmitDto request,
            CancellationToken cancellationToken = default)
        {
            var status = request.Status?.Trim();
            if (!string.Equals(status, Confirmed, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status, Rejected, StringComparison.OrdinalIgnoreCase))
                return Fail("Vui lòng chọn hành động xác nhận hoặc từ chối.");

            var ids = request.ExamInvigilatorIds?.Where(x => x > 0).Distinct().ToList() ?? new List<int>();
            if (!ids.Any())
                return Fail("Vui lòng chọn ít nhất một lịch coi thi.");

            if (string.Equals(status, Rejected, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.Note))
                return Fail("Vui lòng nhập lý do khi từ chối lịch coi thi.");

            var items = await _repository.GetSubmitItemsAsync(ids, cancellationToken);
            if (items.Count != ids.Count || items.Any(x => x.AssigneeId != userId))
                return Fail("Có lịch coi thi không hợp lệ hoặc không thuộc tài khoản của bạn.");

            if (items.Any(x => !string.Equals(x.ScheduleStatus, ExamScheduleStatusHelper.Approved, StringComparison.OrdinalIgnoreCase)))
                return Fail("Chỉ có thể phản hồi các lịch thi đã được duyệt.");

            var note = request.Note?.Trim();
            await _repository.UpsertResponsesAsync(userId, ids, status!, note, cancellationToken);
            await NotifySecretariesAsync(userId, items, status!, note, cancellationToken);

            return new InvigilatorResponseResultDto
            {
                Success = true,
                Message = $"Đã ghi nhận phản hồi cho {items.Count} lịch coi thi."
            };
        }

        public async Task<InvigilatorConfirmationResultDto> SendConfirmationAsync(
            InvigilatorConfirmationRequestDto request,
            string confirmationUrl,
            CancellationToken cancellationToken = default)
        {
            var selectedIds = request.ScheduleIds?.Where(x => x > 0).Distinct().ToList() ?? new List<int>();
            if (!selectedIds.Any())
                return ConfirmationFail("Vui lòng chọn ít nhất một lịch thi đã duyệt để gửi xác nhận.");

            if (request.SecretaryId <= 0)
                return ConfirmationFail("Không xác định được người gửi yêu cầu xác nhận.");

            if (request.FacultyId <= 0)
                return ConfirmationFail("Không xác định được khoa của tài khoản hiện tại.");

            var schedules = await _repository.GetConfirmationSchedulesAsync(selectedIds, cancellationToken);
            var errors = new List<string>();

            if (schedules.Count != selectedIds.Count)
                errors.Add("Một số lịch thi đã chọn không tồn tại hoặc đã bị thay đổi.");

            foreach (var schedule in schedules)
            {
                var label = BuildScheduleLabel(schedule);
                if (schedule.FacultyId != request.FacultyId)
                    errors.Add($"Lịch {label} không thuộc khoa của bạn.");

                if (!string.Equals(schedule.Status, ExamScheduleStatusHelper.Approved, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"Lịch {label} chưa ở trạng thái Đã duyệt.");

                if (schedule.Lecturers.Count < 2)
                    errors.Add($"Lịch {label} chưa đủ 2 giám thị.");

                if (schedule.Lecturers.Any(i => string.IsNullOrWhiteSpace(i.Email)))
                    errors.Add($"Lịch {label} có giám thị chưa có email trong hồ sơ.");
            }

            if (errors.Any())
            {
                return new InvigilatorConfirmationResultDto
                {
                    Success = false,
                    Message = "Chưa thể gửi email xác nhận. Vui lòng kiểm tra lại các lịch đã chọn.",
                    Errors = errors
                };
            }

            var lecturerGroups = schedules
                .SelectMany(schedule => schedule.Lecturers.Select(lecturer => new { Schedule = schedule, Lecturer = lecturer }))
                .GroupBy(x => x.Lecturer.UserId)
                .ToList();

            var sentCount = 0;
            foreach (var group in lecturerGroups)
            {
                var lecturer = group.First().Lecturer;
                var groupSchedules = group.Select(x => x.Schedule).DistinctBy(x => x.ExamScheduleId).ToList();
                var body = BuildConfirmationEmail(lecturer.FullName, groupSchedules, confirmationUrl);

                try
                {
                    await _emailService.SendEmailAsync(lecturer.Email!, "Xác nhận lịch coi thi", body);
                    await _emailLogService.LogAsync(lecturer.UserId, lecturer.Email!, "Sent", null, "InvigilatorConfirmation");
                }
                catch (Exception ex)
                {
                    await _emailLogService.LogAsync(lecturer.UserId, lecturer.Email!, "Failed", ex.Message, "InvigilatorConfirmation");
                    throw;
                }
                await _notificationService.CreateAsync(new NotificationWriteDto
                {
                    UserId = lecturer.UserId,
                    Title = "Lịch coi thi cần xác nhận",
                    Content = $"Bạn có {groupSchedules.Count} lịch coi thi đã được phân công. Vui lòng kiểm tra và phản hồi xác nhận hoặc từ chối trên hệ thống.",
                    Type = NotificationTypes.SchedulePublished,
                    RelatedId = groupSchedules.Count == 1 ? groupSchedules[0].ExamScheduleId : null,
                    CreatedBy = request.SecretaryId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }, cancellationToken);
                sentCount++;
            }

            await _repository.MarkConfirmationSentAsync(schedules.Select(x => x.ExamScheduleId), cancellationToken);

            return new InvigilatorConfirmationResultDto
            {
                Success = true,
                Message = $"Đã gửi email xác nhận đến {sentCount} giảng viên.",
                LecturerCount = sentCount,
                ScheduleCount = schedules.Count
            };
        }

        private async Task NotifySecretariesAsync(
            int lecturerId,
            IReadOnlyList<InvigilatorAssignmentSubmitItemDto> items,
            string status,
            string? note,
            CancellationToken cancellationToken)
        {
            var lecturer = await _repository.GetUserAsync(lecturerId, cancellationToken);
            var secretaries = await _repository.GetActiveSecretariesAsync(items.Select(x => x.FacultyId), cancellationToken);
            var lecturerName = string.IsNullOrWhiteSpace(lecturer?.FullName) ? lecturer?.UserName : lecturer.FullName;
            var relatedId = items.FirstOrDefault()?.ExamScheduleId;
            var content = $"Giảng viên {lecturerName} đã phản hồi {status.ToLower()} {items.Count} lịch coi thi." +
                          (string.IsNullOrWhiteSpace(note) ? string.Empty : $"\nGhi chú: {note.Trim()}");

            foreach (var secretary in secretaries)
            {
                await _notificationService.CreateAsync(new NotificationWriteDto
                {
                    UserId = secretary.UserId,
                    Title = $"Phản hồi lịch coi thi từ {lecturerName}",
                    Content = content,
                    Type = NotificationTypes.InvigilatorResponse,
                    RelatedId = relatedId,
                    CreatedBy = lecturerId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }, cancellationToken);
            }
        }

        private static string BuildScheduleLabel(InvigilatorConfirmationScheduleDto schedule)
        {
            return $"{schedule.SubjectId} - {schedule.ClassName} - Nhóm {schedule.GroupNumber}";
        }

        private static string BuildConfirmationEmail(string lecturerName, IReadOnlyList<InvigilatorConfirmationScheduleDto> schedules, string confirmationUrl)
        {
            static string H(string? value) => HtmlEncoder.Default.Encode(string.IsNullOrWhiteSpace(value) ? "-" : value);

            var rows = string.Join("", schedules.Select(x =>
                $"<tr><td>{H(x.SubjectId)}</td><td>{H(x.SubjectName)}</td><td>{H(x.ClassName)}</td><td>{H(x.GroupNumber)}</td><td>{H($"{x.BuildingId}.{x.RoomName}")}</td><td>{x.ExamDate:dd/MM/yyyy}</td><td>{H(x.SlotName)} ({x.TimeStart:HH\\:mm})</td></tr>"));

            return $@"
                <div style='font-family:Arial,sans-serif;line-height:1.55;color:#172033'>
                    <h2 style='margin:0 0 12px;color:#1d4ed8'>Yêu cầu xác nhận lịch coi thi</h2>
                    <p>Xin chào <strong>{H(lecturerName)}</strong>,</p>
                    <p>Bạn có {schedules.Count} lịch coi thi đã được phân công. Vui lòng kiểm tra và phản hồi xác nhận hoặc từ chối trên hệ thống.</p>
                    <p style='background:#fff7ed;border:1px solid #fed7aa;padding:10px 12px;border-radius:10px;color:#9a3412'><strong>Lưu ý:</strong> Nếu bạn không phản hồi trong vòng 48 giờ kể từ khi nhận thông báo này, hệ thống sẽ tự động ghi nhận là đã xác nhận lịch coi thi.</p>
                    <table style='border-collapse:collapse;width:100%;margin:16px 0;font-size:14px'>
                        <thead><tr style='background:#eff6ff'><th style='border:1px solid #dbeafe;padding:8px'>Mã môn</th><th style='border:1px solid #dbeafe;padding:8px'>Môn học</th><th style='border:1px solid #dbeafe;padding:8px'>Lớp</th><th style='border:1px solid #dbeafe;padding:8px'>Nhóm</th><th style='border:1px solid #dbeafe;padding:8px'>Phòng</th><th style='border:1px solid #dbeafe;padding:8px'>Ngày thi</th><th style='border:1px solid #dbeafe;padding:8px'>Ca thi</th></tr></thead>
                        <tbody>{rows}</tbody>
                    </table>
                    <p><a href='{H(confirmationUrl)}' style='display:inline-block;background:#2563eb;color:white;text-decoration:none;padding:10px 16px;border-radius:10px'>Mở trang xác nhận lịch coi thi</a></p>
                    <p style='color:#64748b;font-size:13px'>Nếu bạn không thể tham gia, hãy chọn từ chối và nhập lý do để Thư ký khoa xử lý thay thế.</p>
                </div>";
        }

        private static InvigilatorResponseResultDto Fail(string message)
        {
            return new InvigilatorResponseResultDto { Success = false, Message = message };
        }

        private static InvigilatorConfirmationResultDto ConfirmationFail(string message)
        {
            return new InvigilatorConfirmationResultDto { Success = false, Message = message };
        }
    }
}
