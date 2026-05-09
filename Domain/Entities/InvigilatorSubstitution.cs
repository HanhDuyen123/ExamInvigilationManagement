namespace ExamInvigilationManagement.Domain.Entities
{
    public class InvigilatorSubstitution
    {
        public int Id { get; set; }

        public int ExamInvigilatorId { get; set; }
        public int UserId { get; set; }
        public int SubstituteUserId { get; set; }

        public string Status { get; set; } = null!;
        public DateTime? CreateAt { get; set; }
    }
}
