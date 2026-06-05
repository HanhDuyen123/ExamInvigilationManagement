using ExamInvigilationManagement.Application.DTOs.ManualAssignment;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IManualAssignmentRepository
    {
        Task<int?> GetUserFacultyIdAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<ManualAssignmentScheduleDto?> GetScheduleAsync(
            int scheduleId,
            int facultyId,
            CancellationToken cancellationToken = default);

        Task<List<ManualAssignmentCurrentInvigilatorDto>> GetCurrentInvigilatorsAsync(
            int scheduleId,
            CancellationToken cancellationToken = default);

        Task<List<ManualAssignmentActivityLogDto>> GetActivityLogsAsync(
            int scheduleId,
            CancellationToken cancellationToken = default);

        Task<List<ManualAssignmentLecturerOptionDto>> GetActiveLecturersAsync(
            int facultyId,
            string subjectId,
            int ownerUserId,
            CancellationToken cancellationToken = default);

        Task<Dictionary<int, int>> GetLecturerLoadsAsync(
            int semesterId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default);

        Task<Dictionary<int, int>> GetSameDayLoadsAsync(
            int semesterId,
            IEnumerable<int> userIds,
            DateTime examDate,
            CancellationToken cancellationToken = default);

        Task<HashSet<int>> GetSubjectLecturerIdsAsync(
            string subjectId,
            CancellationToken cancellationToken = default);

        Task<List<int>> GetBusyLecturerIdsAsync(
            IEnumerable<int> userIds,
            int slotId,
            DateOnly examDate,
            CancellationToken cancellationToken = default);

        Task<HashSet<int>> GetPeriodAvailableLecturerIdsAsync(
            int periodId,
            int facultyId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default);

        Task<bool> HasPeriodAvailabilityListAsync(
            int periodId,
            int facultyId,
            CancellationToken cancellationToken = default);

        Task<HashSet<int>> GetApprovedBusyPeriodLecturerIdsAsync(
            int periodId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default);

        Task<List<int>> GetConflictingLecturerIdsAsync(
            int scheduleId,
            int slotId,
            DateTime examDate,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            ManualAssignmentSavePlanDto plan,
            CancellationToken cancellationToken = default);
    }
}
