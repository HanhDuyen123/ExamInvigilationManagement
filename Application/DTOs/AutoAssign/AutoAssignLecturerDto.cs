namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignLecturerDto
    {
        public int UserId { get; set; }
        public int InformationId { get; set; }
        public int PersonKey => InformationId > 0 ? InformationId : UserId;
        public string UserName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public int? FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;
        public bool IsLecturerRole => string.Equals(RoleName, "Giảng viên", StringComparison.OrdinalIgnoreCase);

        public bool IsActive { get; set; }
    }
}
