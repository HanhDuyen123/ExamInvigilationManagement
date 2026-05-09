namespace ExamInvigilationManagement.Application.DTOs.Admin.User
{
    public class UpdateUserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public byte RoleId { get; set; }
        public int? FacultyId { get; set; }

        public bool IsActive { get; set; }
    }
}
