namespace ExamInvigilationManagement.Application.DTOs.ExamSchedule
{
    public class ExamScheduleValidationContextDto
    {
        public int AcademyYearId { get; set; }
        public int SemesterId { get; set; }
        public int PeriodId { get; set; }
        public int SessionId { get; set; }

        public string? SubjectId { get; set; }
        public int? FacultyId { get; set; }
        public int? UserId { get; set; }

        public string? ClassName { get; set; }
        public string? GroupNumber { get; set; }
    }
}
