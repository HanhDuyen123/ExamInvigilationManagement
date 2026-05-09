namespace ExamInvigilationManagement.Domain.Entities
{
    public class InvigilatorResponse
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int ExamInvigilatorId { get; set; }

        public string Status { get; set; } = null!;
        public string? Note { get; set; }

        public DateTime? ResponseAt { get; set; }
    }
}
