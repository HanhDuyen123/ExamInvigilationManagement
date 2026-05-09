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
        public string? ExamSlotName { get; set; }

        public DateOnly BusyDate { get; set; }
        public string? Note { get; set; }
        public DateTime? CreateAt { get; set; }
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
    }
}
