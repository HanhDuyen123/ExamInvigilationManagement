using ExamInvigilationManagement.Application.DTOs.InvigilatorResponse;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IInvigilatorResponseRepository
    {
        Task<PagedResult<InvigilatorAssignmentItemDto>> GetAssignmentsAsync(
            int userId,
            InvigilatorAssignmentSearchDto search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<List<InvigilatorAssignmentSubmitItemDto>> GetSubmitItemsAsync(
            IEnumerable<int> examInvigilatorIds,
            CancellationToken cancellationToken = default);

        Task UpsertResponsesAsync(
            int userId,
            IEnumerable<int> examInvigilatorIds,
            string status,
            string? note,
            CancellationToken cancellationToken = default);

        Task<InvigilatorNotificationUserDto?> GetUserAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<List<InvigilatorNotificationUserDto>> GetActiveSecretariesAsync(
            IEnumerable<int> facultyIds,
            CancellationToken cancellationToken = default);

        Task<List<InvigilatorConfirmationScheduleDto>> GetConfirmationSchedulesAsync(
            IEnumerable<int> scheduleIds,
            CancellationToken cancellationToken = default);

        Task MarkConfirmationSentAsync(IEnumerable<int> scheduleIds, CancellationToken cancellationToken = default);
        Task<int> AutoConfirmExpiredAsync(TimeSpan responseWindow, CancellationToken cancellationToken = default);
    }
}
