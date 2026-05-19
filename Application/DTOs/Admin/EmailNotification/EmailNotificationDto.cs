namespace ExamInvigilationManagement.Application.DTOs.Admin.EmailNotification
{
    public class EmailNotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? FacultyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Status { get; set; }
        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Type { get; set; }
    }
}
