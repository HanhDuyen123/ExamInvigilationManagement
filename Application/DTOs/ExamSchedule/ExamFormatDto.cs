namespace ExamInvigilationManagement.Application.DTOs.ExamSchedule
{
    public class ExamFormatDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} - {Name}";
    }
}
