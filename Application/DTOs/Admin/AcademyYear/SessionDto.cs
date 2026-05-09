namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class SessionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<SlotDto> Slots { get; set; } = new();
    }
}
