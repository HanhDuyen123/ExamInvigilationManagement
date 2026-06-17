using ExamInvigilationManagement.Application.DTOs.Admin.EmailNotification;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service;

public interface IEmailNotificationAdminService
{
    Task<List<EmailUserSearchDto>> SearchUsersAsync(string? keyword, CancellationToken cancellationToken = default);
    Task<PagedResult<EmailNotificationDto>> GetPagedAsync(string? keyword, int? userId, int? facultyId, string? status, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<EmailNotificationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
