using ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Domain.Entities
{
    public class ExamPeriod
    {
        public int Id { get; set; }
        public int SemesterId { get; set; }
        public string Name { get; set; } = null!;

        public Semester Semester { get; set; } = null!;

        public List<ExamSession> ExamSessions { get; set; } = new();
        public List<ExamSchedule> ExamSchedules { get; set; } = new();
    }
}
