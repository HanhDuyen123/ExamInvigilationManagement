using ExamInvigilationManagement.Application.DTOs.AutoAssign;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IAutoAssignmentRepository
    {
        Task<int?> GetUserFacultyIdAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<List<AutoAssignScheduleDto>> GetSchedulesAsync(
            int semesterId,
            int periodId,
            int facultyId,
            CancellationToken cancellationToken = default);

        Task<List<AutoAssignLecturerDto>> GetActiveLecturersAsync(
            int facultyId,
            IEnumerable<string> subjectIds,
            IEnumerable<int> ownerUserIds,
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

        Task<Dictionary<int, int>> GetLecturerLoadsAsync(
            int semesterId,
            IEnumerable<int> userIds,
            CancellationToken cancellationToken = default);

        Task<Dictionary<string, HashSet<int>>> GetSubjectLecturerMapAsync(
            IEnumerable<string> subjectIds,
            CancellationToken cancellationToken = default);

        Task<List<AutoAssignBusySlotDto>> GetBusySlotsAsync(
            IEnumerable<int> userIds,
            IEnumerable<int> slotIds,
            IEnumerable<DateOnly> busyDates,
            CancellationToken cancellationToken = default);

        Task<List<AutoAssignExistingAssignmentDto>> GetExistingAssignmentsAsync(
            IEnumerable<int> examScheduleIds,
            CancellationToken cancellationToken = default);

        Task SavePlanAsync(
            AutoAssignPlanDto plan,
            CancellationToken cancellationToken = default);
    }
}
