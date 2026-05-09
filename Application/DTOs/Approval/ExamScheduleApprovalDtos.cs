using System.ComponentModel.DataAnnotations;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.DTOs.Approval
{
    public class ApprovalUserContextDto
    {
        public int UserId { get; set; }
        public int? FacultyId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class ExamScheduleApprovalSearchDto
    {
        public string? Lookup { get; set; }

        public int? LecturerId { get; set; }
        public string? SubjectId { get; set; }

        public int? AcademyYearId { get; set; }
        public int? SemesterId { get; set; }
        public int? PeriodId { get; set; }
        public int? SessionId { get; set; }
        public int? SlotId { get; set; }

        public string? BuildingId { get; set; }
        public int? RoomId { get; set; }

        public string? Status { get; set; } = "Chờ duyệt";
    }

    public class ExamScheduleApprovalIndexItemDto
    {
        public int ExamScheduleId { get; set; }

        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GroupNumber { get; set; } = string.Empty;

        public int AcademyYearId { get; set; }
        public string AcademyYearName { get; set; } = string.Empty;

        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;

        public int PeriodId { get; set; }
        public string PeriodName { get; set; } = string.Empty;

        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;

        public int SlotId { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public TimeOnly TimeStart { get; set; }

        public int RoomId { get; set; }
        public string BuildingId { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomDisplay { get; set; } = string.Empty;

        public DateTime ExamDate { get; set; }

        public int? Invigilator1Id { get; set; }
        public int? Invigilator2Id { get; set; }
        public string Invigilator1Name { get; set; } = string.Empty;
        public string Invigilator2Name { get; set; } = string.Empty;

        public int CurrentInvigilatorCount { get; set; }
        public int ApprovalCount { get; set; }

        public string Status { get; set; } = string.Empty;
        public bool CanReview { get; set; }
        public string ReviewReason { get; set; } = string.Empty;
    }

    public class ExamScheduleApprovalIndexPageDto
    {
        public ExamScheduleApprovalSearchDto Search { get; set; } = new();

        public List<int> SelectedExamScheduleIds { get; set; } = new();
        public string? BulkNote { get; set; }

        public int TotalCount { get; set; }
        public int ReviewableCount { get; set; }
        public int NotEnoughCount { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public PagedResult<ExamScheduleApprovalIndexItemDto> PagedItems { get; set; } = new();

        public List<string> StatusOptions { get; set; } = new()
    {
        "Tất cả",
        "Chờ duyệt",
        "Đã duyệt",
        "Từ chối duyệt"
    };
    }

    public class ExamScheduleApprovalBulkReviewRequestDto
    {
        [Required]
        public List<int> SelectedExamScheduleIds { get; set; } = new();

        [Required]
        public bool IsApproved { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }

    public class ExamScheduleApprovalSaveItemDto
    {
        public int ExamScheduleId { get; set; }
        public int ApproverId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime ApproveAt { get; set; } = DateTime.Now;
        public DateTime UpdateAt { get; set; } = DateTime.Now;
    }

    public class ExamScheduleApprovalSavePlanDto
    {
        public List<ExamScheduleApprovalSaveItemDto> Items { get; set; } = new();
    }

    public class ExamScheduleApprovalBulkReviewResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int NotificationsSent { get; set; }
        public string StatusAfter { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
    public class ExamScheduleApprovalPageResultDto
    {
        public PagedResult<ExamScheduleApprovalIndexItemDto> PagedItems { get; set; } = new();
        public int TotalCount { get; set; }
        public int ReviewableCount { get; set; }
        public int NotEnoughCount { get; set; }
    }
}