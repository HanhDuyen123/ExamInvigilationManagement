namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface INotificationRealtimePublisher
    {
        Task PublishToUserAsync(int userId, CancellationToken cancellationToken = default);
        Task PublishToUsersAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default);
    }
}