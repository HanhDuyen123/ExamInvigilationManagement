namespace ExamInvigilationManagement.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public byte RoleId { get; set; }
        public int InformationId { get; set; }
        public int? FacultyId { get; set; }

        public string UserName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public bool IsActive { get; set; }
        public int? FailedLoginAttempts { get; set; }

        public DateTime? LastLogin { get; set; }
        public DateTime? LockoutEnd { get; set; }

        // Navigation (giữ nhưng không ép map)
        public Role? Role { get; set; }
        public Information? Information { get; set; }
        public Faculty? Faculty { get; set; }

        public void IncreaseFailedLogin()
        {
            FailedLoginAttempts = (FailedLoginAttempts ?? 0) + 1;
        }

        public void ResetFailedLogin()
        {
            FailedLoginAttempts = 0;
            LockoutEnd = null;
        }

        public bool IsLocked()
        {
            return LockoutEnd.HasValue && LockoutEnd > DateTime.Now;
        }
    }
}
