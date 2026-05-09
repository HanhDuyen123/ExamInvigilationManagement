namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignLecturerDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public int? FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}