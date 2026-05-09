namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class AcademyYearDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<SemesterDto> Semesters { get; set; } = new();
    }
}
