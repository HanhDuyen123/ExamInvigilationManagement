namespace ExamInvigilationManagement.Application.DTOs.Admin.User
{
    public class UserListDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }

        public string RoleName { get; set; }
        public string FacultyName { get; set; }

        public bool IsActive { get; set; }
    }
}
