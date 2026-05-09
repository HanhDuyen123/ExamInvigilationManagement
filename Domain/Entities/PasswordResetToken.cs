namespace ExamInvigilationManagement.Domain.Entities
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Token { get; set; } = null!;

        public DateTime ExpiredAt { get; set; }

        public bool IsUsed { get; set; }

        public User User { get; set; } = null!;
    }
}
