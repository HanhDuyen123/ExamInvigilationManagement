namespace ExamInvigilationManagement.Application.Interfaces.Common;

public interface IRequestContextService
{
    string BuildAbsoluteUrl(string path);
    Task SignOutAsync();
}
