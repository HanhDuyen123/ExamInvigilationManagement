using ExamInvigilationManagement.Infrastructure.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using ExamInvigilationManagement.Application.Interfaces.Service;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {

        if (string.IsNullOrEmpty(_settings.SenderEmail))
            throw new Exception("SenderEmail is null");

        if (string.IsNullOrEmpty(_settings.Password))
            throw new Exception("Email Password is null");

        using var smtp = new SmtpClient(_settings.SmtpServer, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password),
            EnableSsl = true
        };

        using var mail = new MailMessage
        {
            From = new MailAddress("hanhduyentranthi109@gmail.com", _settings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        
        mail.To.Add(to);

        await smtp.SendMailAsync(mail);
    }

    public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentFileName, byte[] attachmentBytes, string contentType, string? replyTo = null)
    {
        if (string.IsNullOrEmpty(_settings.SenderEmail))
            throw new Exception("SenderEmail is null");

        if (string.IsNullOrEmpty(_settings.Password))
            throw new Exception("Email Password is null");

        using var smtp = new SmtpClient(_settings.SmtpServer, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password),
            EnableSsl = true
        };

        using var mail = new MailMessage
        {
            From = new MailAddress("hanhduyentranthi109@gmail.com", _settings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(to);

        if (!string.IsNullOrWhiteSpace(replyTo))
            mail.ReplyToList.Add(new MailAddress(replyTo));

        var stream = new MemoryStream(attachmentBytes);
        mail.Attachments.Add(new Attachment(stream, attachmentFileName, contentType));

        await smtp.SendMailAsync(mail);
    }
}
