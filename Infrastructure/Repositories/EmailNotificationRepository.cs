using ExamInvigilationManagement.Application.DTOs.Admin.EmailNotification;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories;

public class EmailNotificationRepository : IEmailNotificationRepository
{
    private readonly ApplicationDbContext _db;

    public EmailNotificationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmailUserSearchDto>> SearchUsersAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsNoTracking().Include(x => x.Information).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x => x.UserName.Contains(kw) || x.Information.FirstName.Contains(kw) || x.Information.LastName.Contains(kw));
        }

        return await query
            .OrderBy(x => x.UserName)
            .Take(20)
            .Select(x => new EmailUserSearchDto
            {
                Id = x.UserId,
                Name = x.UserName + " - " + (x.Information.LastName + " " + x.Information.FirstName).Trim()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<EmailNotificationDto>> GetPagedAsync(string? keyword, int? userId, int? facultyId, string? status, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.EmailNotifications
            .AsNoTracking()
            .Include(x => x.User)
                .ThenInclude(x => x.Information)
            .Include(x => x.User)
                .ThenInclude(x => x.Faculty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x =>
                x.Email.Contains(kw) ||
                (x.Type != null && x.Type.Contains(kw)) ||
                (x.ErrorMessage != null && x.ErrorMessage.Contains(kw)) ||
                x.User.UserName.Contains(kw) ||
                x.User.Information.FirstName.Contains(kw) ||
                x.User.Information.LastName.Contains(kw));
        }

        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (facultyId.HasValue) query = query.Where(x => x.User.FacultyId == facultyId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type);
        if (fromDate.HasValue) query = query.Where(x => x.SentAt >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.SentAt < toDate.Value.Date.AddDays(1));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.SentAt)
            .ThenByDescending(x => x.EmailId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmailNotificationDto
            {
                Id = x.EmailId,
                UserId = x.UserId,
                UserName = x.User.UserName,
                FullName = (x.User.Information.LastName + " " + x.User.Information.FirstName).Trim(),
                FacultyName = x.User.Faculty != null ? x.User.Faculty.FacultyName : null,
                Email = x.Email,
                Status = x.Status,
                SentAt = x.SentAt,
                ErrorMessage = x.ErrorMessage,
                Type = x.Type
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<EmailNotificationDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<EmailNotificationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.EmailNotifications
            .AsNoTracking()
            .Include(x => x.User)
                .ThenInclude(x => x.Information)
            .Include(x => x.User)
                .ThenInclude(x => x.Faculty)
            .Where(x => x.EmailId == id)
            .Select(x => new EmailNotificationDto
            {
                Id = x.EmailId,
                UserId = x.UserId,
                UserName = x.User.UserName,
                FullName = (x.User.Information.LastName + " " + x.User.Information.FirstName).Trim(),
                FacultyName = x.User.Faculty != null ? x.User.Faculty.FacultyName : null,
                Email = x.Email,
                Status = x.Status,
                SentAt = x.SentAt,
                ErrorMessage = x.ErrorMessage,
                Type = x.Type
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
