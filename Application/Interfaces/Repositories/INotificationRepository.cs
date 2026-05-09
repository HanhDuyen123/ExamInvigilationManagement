using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task CreateAsync(
            NotificationWriteDto dto,
            CancellationToken cancellationToken = default);

        Task UpsertAsync(
            NotificationWriteDto dto,
            CancellationToken cancellationToken = default);

        Task<PagedResult<NotificationListItemDto>> GetPagedAsync(
            int userId,
            bool canViewAll,
            NotificationSearchDto search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<List<NotificationListItemDto>> GetRecentAsync(
            int userId,
            int take = 5,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<NotificationDetailDto?> GetByIdAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default);

        Task<int> MarkAllAsReadAsync(
            int userId,
            CancellationToken cancellationToken = default);
    }
}
