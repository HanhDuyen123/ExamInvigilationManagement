namespace ExamInvigilationManagement.Application.DTOs
{
    public class ProfileDto
    {
        public string UserName { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime? Dob { get; set; }

        public string? Phone { get; set; }
        public string? Address { get; set; }

        public string Email { get; set; }

        public string? Avt { get; set; }
        public string? Gender { get; set; }

        public string? PositionName { get; set; }
        public bool IsActive { get; set; }
    }
}
