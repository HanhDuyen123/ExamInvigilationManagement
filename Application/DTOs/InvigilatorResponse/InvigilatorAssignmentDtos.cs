using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.DTOs.InvigilatorResponse
{
    public class InvigilatorAssignmentSearchDto
    {
        public string? Keyword { get; set; }
        public string? SubjectId { get; set; }
        public string? BuildingId { get; set; }
        public int? RoomId { get; set; }
        public int? AcademyYearId { get; set; }
        public int? SemesterId { get; set; }
        public int? PeriodId { get; set; }
        public int? SessionId { get; set; }
        public int? SlotId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class InvigilatorAssignmentItemDto
    {
        public int ExamInvigilatorId { get; set; }
        public int ExamScheduleId { get; set; }
        public byte PositionNo { get; set; }
        public string? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public string? ClassName { get; set; }
        public string? GroupNumber { get; set; }
        public string? BuildingId { get; set; }
        public string? RoomName { get; set; }
        public string RoomDisplay => string.IsNullOrWhiteSpace(BuildingId) ? (RoomName ?? "-") : $"{BuildingId}.{RoomName}";
        public string? AcademyYearName { get; set; }
        public string? SemesterName { get; set; }
        public string? PeriodName { get; set; }
        public string? SessionName { get; set; }
        public string? SlotName { get; set; }
        public TimeOnly? TimeStart { get; set; }
        public DateTime? ExamDate { get; set; }
        public string? Lecturer1Name { get; set; }
        public string? Lecturer2Name { get; set; }
        public string ResponseStatus { get; set; } = "Chưa phản hồi";
        public string? ResponseNote { get; set; }
        public bool HasSubstitutionProposal { get; set; }
        public string SubstitutionStatus { get; set; } = string.Empty;
    }

    public class InvigilatorAssignmentIndexDto
    {
        public InvigilatorAssignmentSearchDto Search { get; set; } = new();
        public PagedResult<InvigilatorAssignmentItemDto> PagedItems { get; set; } = new();
    }

    public class InvigilatorResponseSubmitDto
    {
        public List<int> ExamInvigilatorIds { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public class InvigilatorResponseResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }

    public class InvigilatorAssignmentSubmitItemDto
    {
        public int ExamInvigilatorId { get; set; }
        public int ExamScheduleId { get; set; }
        public int AssigneeId { get; set; }
        public int FacultyId { get; set; }
        public string ScheduleStatus { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
    }

    public class InvigilatorNotificationUserDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }

    public class InvigilatorConfirmationRequestDto
    {
        public List<int> ScheduleIds { get; set; } = new();
        public int SecretaryId { get; set; }
        public int FacultyId { get; set; }
    }

    public class InvigilatorConfirmationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public int LecturerCount { get; set; }
        public int ScheduleCount { get; set; }
    }

    public class InvigilatorConfirmationScheduleDto
    {
        public int ExamScheduleId { get; set; }
        public int FacultyId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GroupNumber { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public TimeOnly? TimeStart { get; set; }
        public List<InvigilatorConfirmationLecturerDto> Lecturers { get; set; } = new();
    }

    public class InvigilatorConfirmationLecturerDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
