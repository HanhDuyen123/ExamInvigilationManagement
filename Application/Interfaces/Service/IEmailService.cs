namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentFileName, byte[] attachmentBytes, string contentType, string? replyTo = null);
    }
}
