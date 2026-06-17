namespace ExamInvigilationManagement.Application.DTOs.ExamSchedule
{
    public class ExamScheduleSupportRequestDocumentDto
    {
        public string FacultyName { get; set; } = string.Empty;
        public string AcademyYearName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<ExamScheduleDto> Schedules { get; set; } = new();
    }
}
