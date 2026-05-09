namespace ExamInvigilationManagement.Application.DTOs.Admin.User
{
    public class UserFilterDto
    {
        public string? Keyword { get; set; }
        public int? RoleId { get; set; }
        public int? FacultyId { get; set; }
        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 2;
    }
}
