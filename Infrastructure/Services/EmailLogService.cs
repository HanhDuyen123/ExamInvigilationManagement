namespace ExamInvigilationManagement.Infrastructure.Services;

using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Data.Entities;

public class EmailLogService : IEmailLogService
{
    private readonly ApplicationDbContext _context;

    public EmailLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int userId, string email, string status, string? errorMessage, string type)
    {
        var log = new EmailNotification
        {
            UserId = userId,
            Email = email,
            Status = status,
            SentAt = DateTime.Now,
            ErrorMessage = errorMessage,
            Type = type
        };

        _context.EmailNotifications.Add(log);
        await _context.SaveChangesAsync();
    }
}