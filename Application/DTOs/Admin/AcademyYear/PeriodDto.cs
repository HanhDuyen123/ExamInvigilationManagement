namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class PeriodDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<SessionDto> Sessions { get; set; } = new();
    }
}
