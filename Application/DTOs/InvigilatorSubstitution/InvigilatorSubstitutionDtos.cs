using ExamInvigilationManagement.Application.DTOs.ManualAssignment;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.DTOs.InvigilatorSubstitution
{
    public class InvigilatorSubstitutionScheduleDto
    {
        public int ExamInvigilatorId { get; set; }
        public int ExamScheduleId { get; set; }
        public byte PositionNo { get; set; }
        public int CurrentAssigneeId { get; set; }
        public string CurrentAssigneeName { get; set; } = string.Empty;
        public int FacultyId { get; set; }
        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GroupNumber { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public int SemesterId { get; set; }
        public int PeriodId { get; set; }
        public int SessionId { get; set; }
        public int SlotId { get; set; }
        public int OfferingUserId { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public TimeOnly TimeStart { get; set; }
        public string RoomDisplay { get; set; } = string.Empty;
        public string ScheduleStatus { get; set; } = string.Empty;
        public string ResponseStatus { get; set; } = string.Empty;
        public string? ResponseNote { get; set; }
    }

    public class InvigilatorSubstitutionCreatePageDto
    {
        public InvigilatorSubstitutionScheduleDto Schedule { get; set; } = new();
        public List<ManualAssignmentLecturerOptionDto> LecturerOptions { get; set; } = new();
        public InvigilatorSubstitutionCreateRequestDto Request { get; set; } = new();
    }

    public class InvigilatorSubstitutionCreateRequestDto
    {
        public int ExamInvigilatorId { get; set; }
        public int SubstituteUserId { get; set; }
    }

    public class InvigilatorSubstitutionReviewRequestDto
    {
        public int SubstitutionId { get; set; }
    }

    public class InvigilatorSubstitutionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public int? RelatedId { get; set; }
    }

    public class InvigilatorSubstitutionSearchDto
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class InvigilatorSubstitutionListItemDto
    {
        public int SubstitutionId { get; set; }
        public int ExamInvigilatorId { get; set; }
        public int ExamScheduleId { get; set; }
        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GroupNumber { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public TimeOnly TimeStart { get; set; }
        public string RoomDisplay { get; set; } = string.Empty;
        public byte PositionNo { get; set; }
        public string RequestUserName { get; set; } = string.Empty;
        public string SubstituteUserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CreateAt { get; set; }
    }

    public class InvigilatorSubstitutionDetailDto : InvigilatorSubstitutionListItemDto
    {
        public string RequestUserAccount { get; set; } = string.Empty;
        public string SubstituteUserAccount { get; set; } = string.Empty;
        public int SubstituteUserId { get; set; }
        public string ScheduleStatus { get; set; } = string.Empty;
        public string ResponseStatus { get; set; } = string.Empty;
        public string? ResponseNote { get; set; }
        public bool CanApprove { get; set; }
        public string ApproveReason { get; set; } = string.Empty;
        public ManualAssignmentLecturerOptionDto? SubstituteEvaluation { get; set; }
        public List<ManualAssignmentLecturerOptionDto> ReplacementOptions { get; set; } = new();
    }

    public class InvigilatorSubstitutionIndexPageDto
    {
        public InvigilatorSubstitutionSearchDto Search { get; set; } = new();
        public PagedResult<InvigilatorSubstitutionListItemDto> PagedItems { get; set; } = new();
    }
}
