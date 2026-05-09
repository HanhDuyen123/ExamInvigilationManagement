namespace ExamInvigilationManagement.Domain.Entities
{
    public class EmailNotification
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Type { get; set; }
    }
}
