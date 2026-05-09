namespace ExamInvigilationManagement.Application.DTOs.Admin.User
{
    public class EditUserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }

        public byte RoleId { get; set; }
        public string RoleName { get; set; }

        public int? FacultyId { get; set; }
        public string FacultyName { get; set; }

        public bool IsActive { get; set; }

        public List<RoleItem> Roles { get; set; } = new();
        public List<FacultyItem> Faculties { get; set; } = new();
    }
    public class RoleItem
    {
        public byte Id { get; set; }
        public string Name { get; set; }
    }

    public class FacultyItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
