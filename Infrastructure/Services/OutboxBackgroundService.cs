using System.Text.Json;
using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Services;

public sealed class OutboxBackgroundService : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxRetries = 5;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xử lý outbox batch.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailLogService = scope.ServiceProvider.GetRequiredService<IEmailLogService>();

        var messages = await db.OutboxMessages
            .Where(x => x.Status == "Pending" && x.RetryCount < MaxRetries)
            .OrderBy(x => x.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var payload = ParsePayload(message.Payload);
                if (payload == null)
                    throw new InvalidOperationException("Payload outbox không đọc được.");

                var title = string.IsNullOrWhiteSpace(payload.Title) ? BuildDefaultTitle(message.Type) : payload.Title;
                var content = string.IsNullOrWhiteSpace(payload.Content) ? BuildDefaultContent(message.Type, payload) : payload.Content;
                var notificationSent = false;

                if (payload.RecipientIds.Count > 0)
                {
                    foreach (var recipientId in payload.RecipientIds.Where(x => x > 0).Distinct())
                    {
                        await notificationService.CreateAsync(new NotificationWriteDto
                        {
                            UserId = recipientId,
                            Title = title,
                            Content = content,
                            Type = string.IsNullOrWhiteSpace(payload.NotificationType) ? NotificationTypes.System : payload.NotificationType,
                            RelatedId = payload.RelatedId,
                            CreatedBy = payload.CreatedBy,
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        }, cancellationToken);
                    }

                    notificationSent = true;
                }

                var emailRecipients = await BuildEmailRecipientsAsync(db, payload, cancellationToken);
                if (emailRecipients.Count > 0)
                {
                    var body = string.IsNullOrWhiteSpace(payload.EmailBody) ? BuildEmailBody(title, content) : payload.EmailBody;
                    foreach (var recipient in emailRecipients)
                    {
                        try
                        {
                            await emailService.SendEmailAsync(recipient.Email, title, body);
                            await emailLogService.LogAsync(recipient.UserId, recipient.Email, "Sent", null, message.Type);
                        }
                        catch (Exception emailEx)
                        {
                            await emailLogService.LogAsync(recipient.UserId, recipient.Email, "Failed", emailEx.Message, message.Type);
                            if (payload.RequireEmailSuccess)
                                throw;
                        }
                    }
                }

                message.Status = "Processed";
                message.ProcessedAt = DateTime.Now;
                message.ErrorMessage = notificationSent || emailRecipients.Count > 0 ? null : "Processed without recipients.";
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Status = message.RetryCount >= MaxRetries ? "Failed" : "Pending";
                message.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Xử lý outbox {OutboxMessageId} thất bại.", message.OutboxMessageId);
            }
        }

        if (messages.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static OutboxNotificationPayload? ParsePayload(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<OutboxNotificationPayload>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static string BuildDefaultTitle(string type) => type switch
    {
        "ApprovalRequestCreated" => "Có yêu cầu duyệt lịch thi mới",
        "ApprovalReviewed" => "Lịch thi đã được xử lý duyệt",
        "AutoAssignmentSaved" => "Tự động phân công đã hoàn tất",
        "InvigilatorSubstitutionApproved" => "Đề xuất thay thế đã được duyệt",
        _ => "Thông báo hệ thống"
    };

    private static string BuildDefaultContent(string type, OutboxNotificationPayload payload) => type switch
    {
        "ApprovalRequestCreated" => $"Hệ thống đã ghi nhận yêu cầu duyệt {payload.ScheduleCountText}.",
        "ApprovalReviewed" => $"Hệ thống đã ghi nhận kết quả duyệt {payload.ProcessedCountText}.",
        "AutoAssignmentSaved" => $"Hệ thống đã lưu phương án phân công {payload.AssignmentCountText}.",
        "InvigilatorSubstitutionApproved" => "Hệ thống đã ghi nhận thay đổi giám thị thay thế.",
        _ => "Một nghiệp vụ hệ thống đã được xử lý."
    };

    private static async Task<List<EmailRecipient>> BuildEmailRecipientsAsync(
        ApplicationDbContext db,
        OutboxNotificationPayload payload,
        CancellationToken cancellationToken)
    {
        var recipients = new List<EmailRecipient>();

        foreach (var item in payload.EmailRecipients.Where(x => !string.IsNullOrWhiteSpace(x.Email)))
            recipients.Add(new EmailRecipient(item.UserId, item.Email.Trim()));

        if (payload.SendEmail && payload.RecipientIds.Any(x => x > 0))
        {
            var ids = payload.RecipientIds.Where(x => x > 0).Distinct().ToList();
            var users = await db.Users
                .AsNoTracking()
                .Include(x => x.Information)
                .Where(x => ids.Contains(x.UserId) && x.IsActive && x.Information.Email != null && x.Information.Email != "")
                .Select(x => new EmailRecipient(x.UserId, x.Information.Email))
                .ToListAsync(cancellationToken);

            recipients.AddRange(users);
        }

        return recipients
            .GroupBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static string BuildEmailBody(string title, string content)
    {
        return $"""
        <div style="font-family:Arial,sans-serif;line-height:1.6;color:#0f172a">
            <h2 style="margin:0 0 12px">{System.Net.WebUtility.HtmlEncode(title)}</h2>
            <p style="margin:0 0 16px">{System.Net.WebUtility.HtmlEncode(content)}</p>
            <p style="margin:0;color:#64748b;font-size:13px">Email này được gửi tự động từ hệ thống quản lý coi thi.</p>
        </div>
        """;
    }

    private sealed class OutboxNotificationPayload
    {
        public List<int> RecipientIds { get; set; } = new();
        public List<EmailRecipientPayload> EmailRecipients { get; set; } = new();
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? EmailBody { get; set; }
        public string? NotificationType { get; set; }
        public int? RelatedId { get; set; }
        public int? CreatedBy { get; set; }
        public bool SendEmail { get; set; }
        public bool RequireEmailSuccess { get; set; }
        public int? ScheduleCount { get; set; }
        public int? ProcessedCount { get; set; }
        public int? AssignmentCount { get; set; }

        public string ScheduleCountText => ScheduleCount.HasValue ? $"{ScheduleCount.Value} lịch thi" : "lịch thi";
        public string ProcessedCountText => ProcessedCount.HasValue ? $"{ProcessedCount.Value} lịch thi" : "lịch thi";
        public string AssignmentCountText => AssignmentCount.HasValue ? $"{AssignmentCount.Value} lượt" : "phân công";
    }

    private sealed class EmailRecipientPayload
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    private sealed record EmailRecipient(int UserId, string Email);
}
