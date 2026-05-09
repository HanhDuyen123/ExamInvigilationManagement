namespace ExamInvigilationManagement.Application.Interfaces.Common
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}
