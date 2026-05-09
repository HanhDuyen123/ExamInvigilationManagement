namespace ExamInvigilationManagement.Domain.Entities
{
    public class Subject
    {
        public string Id { get; set; } = null!;
        public int FacultyId { get; set; }

        public string Name { get; set; } = null!;
        public byte Credit { get; set; }

        public Faculty? Faculty { get; set; }
    }
}
