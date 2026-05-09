namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class ExamSlotOptionDto
    {
        public string Name { get; set; } = null!;
        public TimeOnly TimeStart { get; set; }
        public bool Selected { get; set; }
    }
}
