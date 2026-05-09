using ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Domain.Entities
{
    public class AcademyYear
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public List<Semester> Semesters { get; set; } = new();
        public List<ExamSchedule> ExamSchedules { get; set; } = new();
    }
}
