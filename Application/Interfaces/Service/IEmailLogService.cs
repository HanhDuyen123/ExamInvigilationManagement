namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IEmailLogService
    {
        Task LogAsync(int userId, string email, string status, string? errorMessage, string type);
    }
}
