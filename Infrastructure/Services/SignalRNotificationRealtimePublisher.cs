using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ExamInvigilationManagement.Infrastructure.Services
{
    public class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
    {
        private readonly IHubContext<NotificationHub> _hub;

        public SignalRNotificationRealtimePublisher(IHubContext<NotificationHub> hub)
        {
            _hub = hub;
        }

        public async Task PublishToUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var payload = new { userId, changedAt = DateTime.Now };

            await _hub.Clients.Group($"user-{userId}")
                .SendAsync("notification:changed", payload, cancellationToken);

            await _hub.Clients.User(userId.ToString())
                .SendAsync("notification:changed", payload, cancellationToken);
        }

        public async Task PublishToUsersAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
        {
            foreach (var userId in userIds.Distinct())
            {
                await PublishToUserAsync(userId, cancellationToken);
            }
        }
    }
}
