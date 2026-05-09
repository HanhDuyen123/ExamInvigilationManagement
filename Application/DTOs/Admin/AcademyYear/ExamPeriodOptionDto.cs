namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class ExamPeriodOptionDto
    {
        public string Name { get; set; } = null!;
        public bool Selected { get; set; }

        public List<ExamSessionOptionDto> Sessions { get; set; } = new();
    }
}
