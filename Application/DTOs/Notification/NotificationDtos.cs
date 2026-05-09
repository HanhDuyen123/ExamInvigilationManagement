using ExamInvigilationManagement.Common;
using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Notification
{
    public static class NotificationTypes
    {
        public const string All = "";
        public const string ExamScheduleApproval = "ExamScheduleApproval";
        public const string ManualAssignment = "ManualAssignment";
        public const string InvigilatorResponse = "InvigilatorResponse";
        public const string InvigilatorSubstitution = "InvigilatorSubstitution";
        public const string SchedulePublished = "SchedulePublished";
        public const string System = "System";

        public static IReadOnlyList<NotificationTypeOptionDto> Options { get; } = new List<NotificationTypeOptionDto>
        {
            new() { Value = All, Label = "Tất cả" },
            new() { Value = ExamScheduleApproval, Label = "Duyệt lịch thi" },
            new() { Value = ManualAssignment, Label = "Phân công giám thị" },
            new() { Value = InvigilatorResponse, Label = "Phản hồi giám thị" },
            new() { Value = InvigilatorSubstitution, Label = "Đề xuất thay thế" },
            new() { Value = SchedulePublished, Label = "Gửi lịch thi" },
            new() { Value = System, Label = "Thông báo chung" }
        };
    }

    public class NotificationTypeOptionDto
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class NotificationWriteDto
    {
        [Required]
        public int UserId { get; set; }

        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        public bool? IsRead { get; set; } = false;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? RelatedId { get; set; }
        public int? CreatedBy { get; set; }
    }
    public class NotificationSearchDto
    {
        public string? Keyword { get; set; }
        public string? Type { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? RecipientId { get; set; }
        public int? SenderId { get; set; }
    }

    public class NotificationListItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? RelatedId { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public string? RecipientName { get; set; }
    }

    public class NotificationDetailDto : NotificationListItemDto
    {
    }

    public class NotificationIndexPageDto
    {
        public NotificationSearchDto Search { get; set; } = new();

        public PagedResult<NotificationListItemDto> PagedItems { get; set; } = new();

        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public bool IsAdmin { get; set; }
        public List<NotificationTypeOptionDto> TypeOptions { get; set; } = NotificationTypes.Options.ToList();
    }

    public class NotificationMarkReadRequestDto
    {
        [Required]
        public int Id { get; set; }
    }
}
