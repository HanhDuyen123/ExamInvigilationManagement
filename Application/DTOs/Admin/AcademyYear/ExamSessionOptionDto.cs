namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class ExamSessionOptionDto
    {
        public string Name { get; set; } = null!;
        public bool Selected { get; set; }

        public List<ExamSlotOptionDto> Slots { get; set; } = new();
    }
}
