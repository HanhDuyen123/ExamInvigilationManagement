namespace ExamInvigilationManagement.Application.DTOs.Admin.User
{
    public class CreateUserDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public byte RoleId { get; set; }
        public int InformationId { get; set; }
        public int? FacultyId { get; set; }
    }
}
