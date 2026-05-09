namespace ExamInvigilationManagement.Application.DTOs.ExamSchedule
{
    public class ExamScheduleSearchDto
    {
        public string? Keyword { get; set; }

        public int? FacultyId { get; set; }
        public int? UserId { get; set; }

        public int? AcademyYearId { get; set; }
        public int? SemesterId { get; set; }
        public int? PeriodId { get; set; }
        public int? SessionId { get; set; }
        public int? SlotId { get; set; }

        public string? SubjectId { get; set; }
        public string? ClassName { get; set; }
        public string? GroupNumber { get; set; }

        public string? BuildingId { get; set; }
        public int? RoomId { get; set; }

        public string? Status { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        public string? CurrentRole { get; set; }
        public int? CurrentUserId { get; set; }
        public int? CurrentFacultyId { get; set; }
    }
}
