using ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Domain.Entities
{
    public class Semester
    {
        public int Id { get; set; }
        public int AcademyYearId { get; set; }
        public string Name { get; set; } = null!;

        public AcademyYear AcademyYear { get; set; } = null!;

        public List<CourseOffering> CourseOfferings { get; set; } = new();
        public List<ExamPeriod> ExamPeriods { get; set; } = new();
        public List<ExamSchedule> ExamSchedules { get; set; } = new();
    }
}
