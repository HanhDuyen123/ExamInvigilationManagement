using ExamInvigilationManagement.Application.DTOs.InvigilatorSubstitution;
using ExamInvigilationManagement.Application.DTOs.ManualAssignment;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IInvigilatorSubstitutionRepository
    {
        Task<int?> GetUserFacultyIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionScheduleDto?> GetRejectedAssignmentAsync(int examInvigilatorId, int userId, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionScheduleDto?> GetAssignmentForReviewAsync(int examInvigilatorId, CancellationToken cancellationToken = default);
        Task<List<ManualAssignmentLecturerOptionDto>> GetActiveLecturersAsync(int facultyId, CancellationToken cancellationToken = default);
        Task<List<(int UserId, string DisplayName)>> GetActiveSecretariesAsync(int facultyId, CancellationToken cancellationToken = default);
        Task<List<int>> GetBusyLecturerIdsAsync(IEnumerable<int> userIds, int slotId, DateOnly examDate, CancellationToken cancellationToken = default);
        Task<List<int>> GetConflictingLecturerIdsAsync(int scheduleId, int semesterId, int periodId, int sessionId, int slotId, IEnumerable<int> userIds, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetLecturerLoadsAsync(int semesterId, int facultyId, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetPeriodLoadsAsync(int semesterId, int periodId, int facultyId, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetSameDayLoadsAsync(int semesterId, int facultyId, DateTime examDate, CancellationToken cancellationToken = default);
        Task<List<int>> GetSubjectTeacherIdsAsync(int semesterId, string subjectId, int facultyId, CancellationToken cancellationToken = default);
        Task<List<int>> GetClassTeacherIdsAsync(int semesterId, string subjectId, string className, string groupNumber, int facultyId, CancellationToken cancellationToken = default);
        Task<bool> HasAnyAsync(int examInvigilatorId, int userId, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(int examInvigilatorId, int userId, int substituteUserId, CancellationToken cancellationToken = default);
        Task<PagedResult<InvigilatorSubstitutionListItemDto>> GetPagedAsync(int facultyId, InvigilatorSubstitutionSearchDto search, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionDetailDto?> GetDetailAsync(int substitutionId, int facultyId, CancellationToken cancellationToken = default);
        Task ApproveAsync(int substitutionId, int reviewerId, CancellationToken cancellationToken = default);
        Task ApproveWithReplacementAsync(int substitutionId, int replacementUserId, int reviewerId, CancellationToken cancellationToken = default);
        Task RejectAsync(int substitutionId, CancellationToken cancellationToken = default);
    }
}
