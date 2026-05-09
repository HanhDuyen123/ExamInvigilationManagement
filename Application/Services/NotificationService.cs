using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationRealtimePublisher _realtimePublisher;

        public NotificationService(
            INotificationRepository notificationRepository,
            INotificationRealtimePublisher realtimePublisher)
        {
            _notificationRepository = notificationRepository;
            _realtimePublisher = realtimePublisher;
        }

        public async Task CreateAsync(
            NotificationWriteDto dto,
            CancellationToken cancellationToken = default)
        {
            await _notificationRepository.CreateAsync(dto, cancellationToken);
            await _realtimePublisher.PublishToUserAsync(dto.UserId, cancellationToken);
        }

        public async Task UpsertAsync(
            NotificationWriteDto dto,
            CancellationToken cancellationToken = default)
        {
            await _notificationRepository.UpsertAsync(dto, cancellationToken);
            await _realtimePublisher.PublishToUserAsync(dto.UserId, cancellationToken);
        }
        public Task<PagedResult<NotificationListItemDto>> GetPagedAsync(
            int userId,
            bool canViewAll,
            NotificationSearchDto search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return _notificationRepository.GetPagedAsync(userId, canViewAll, search, page, pageSize, cancellationToken);
        }

        public Task<List<NotificationListItemDto>> GetRecentAsync(
            int userId,
            int take = 5,
            CancellationToken cancellationToken = default)
        {
            return _notificationRepository.GetRecentAsync(userId, take, cancellationToken);
        }

        public Task<int> GetUnreadCountAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
        }

        public Task<NotificationDetailDto?> GetByIdAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default)
        {
            return _notificationRepository.GetByIdAsync(id, userId, cancellationToken);
        }

        public async Task<bool> MarkAsReadAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var ok = await _notificationRepository.MarkAsReadAsync(id, userId, cancellationToken);
            if (ok)
                await _realtimePublisher.PublishToUserAsync(userId, cancellationToken);

            return ok;
        }

        public async Task<int> MarkAllAsReadAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var count = await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);
            if (count > 0)
                await _realtimePublisher.PublishToUserAsync(userId, cancellationToken);

            return count;
        }
    }
}
