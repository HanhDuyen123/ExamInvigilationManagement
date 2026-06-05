namespace ExamInvigilationManagement.Application.DTOs.LecturerBusySlot
{
    public class LecturerBusySlotDto
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public string? UserName { get; set; }

        public int? FacultyId { get; set; }
        public string? FacultyName { get; set; }

        public int? AcademyYearId { get; set; }
        public string? AcademyYearName { get; set; }

        public int? SemesterId { get; set; }
        public string? SemesterName { get; set; }

        public int? ExamPeriodId { get; set; }
        public string? ExamPeriodName { get; set; }

        public int? ExamSessionId { get; set; }
        public string? ExamSessionName { get; set; }

        public int? ExamSlotId { get; set; }
        public List<int> ExamSlotIds { get; set; } = new();
        public string? ExamSlotName { get; set; }

        public DateOnly BusyDate { get; set; }
        public string BusyDateDisplay => BusyWholePeriod || BusyDate == default ? "Cả đợt" : BusyDate.ToString("dd/MM/yyyy");
        public string? Note { get; set; }
        public DateTime? CreateAt { get; set; }
        public string ApprovalStatus { get; set; } = "Chờ duyệt";
        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public bool IsFinal => ApprovalStatus == "Đã duyệt" || ApprovalStatus == "Từ chối duyệt";

        public bool BusyWholePeriod { get; set; }
    }

    public class CreateBusySlotDto
    {
        public int SlotId { get; set; }
        public DateOnly BusyDate { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateBusySlotDto : CreateBusySlotDto
    {
        public int Id { get; set; }
    }
    public class LecturerBusySlotSearchDto
    {
        public string? Keyword { get; set; }

        public int? UserId { get; set; }
        public int? FacultyId { get; set; }

        public int? AcademyYearId { get; set; }
        public int? SemesterId { get; set; }
        public int? ExamPeriodId { get; set; }
        public int? ExamSessionId { get; set; }
        public int? ExamSlotId { get; set; }

        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string? ApprovalStatus { get; set; }
    }

    public class LecturerPeriodAvailabilityDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? FacultyId { get; set; }
        public string? FacultyName { get; set; }
        public int PeriodId { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public string AcademyYearName { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class LecturerPeriodAvailabilitySearchDto
    {
        public string? Keyword { get; set; }
        public int? FacultyId { get; set; }
        public int? AcademyYearId { get; set; }
        public int? SemesterId { get; set; }
        public int? PeriodId { get; set; }
    }
}
