namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface INotificationRealtimePublisher
    {
        Task PublishToUserAsync(int userId, string changeKind = "changed", CancellationToken cancellationToken = default);
        Task PublishToUsersAsync(IEnumerable<int> userIds, string changeKind = "changed", CancellationToken cancellationToken = default);
    }
}
