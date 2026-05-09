namespace ExamInvigilationManagement.Application.Interfaces.Common
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string Role { get; }
        int? DepartmentId { get; }
    }
}
