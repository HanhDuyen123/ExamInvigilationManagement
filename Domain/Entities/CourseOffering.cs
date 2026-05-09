namespace ExamInvigilationManagement.Domain.Entities
{
    public class CourseOffering
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int SemesterId { get; set; }
        public string SubjectId { get; set; } = null!;

        public string ClassName { get; set; } = null!;
        public string GroupNumber { get; set; } = null!;

        public User? User { get; set; }
        public Semester? Semester { get; set; }
        public Subject? Subject { get; set; }
    }
}
