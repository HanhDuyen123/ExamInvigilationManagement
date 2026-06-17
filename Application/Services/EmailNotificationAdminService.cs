using ExamInvigilationManagement.Application.DTOs.Admin.EmailNotification;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Services;

public class EmailNotificationAdminService : IEmailNotificationAdminService
{
    private readonly IEmailNotificationRepository _repository;

    public EmailNotificationAdminService(IEmailNotificationRepository repository)
    {
        _repository = repository;
    }

    public Task<List<EmailUserSearchDto>> SearchUsersAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        return _repository.SearchUsersAsync(keyword, cancellationToken);
    }

    public Task<PagedResult<EmailNotificationDto>> GetPagedAsync(string? keyword, int? userId, int? facultyId, string? status, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return _repository.GetPagedAsync(keyword, userId, facultyId, status, type, fromDate, toDate, page, pageSize, cancellationToken);
    }

    public Task<EmailNotificationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }
}
