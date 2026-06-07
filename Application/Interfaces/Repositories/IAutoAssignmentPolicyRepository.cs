using ExamInvigilationManagement.Application.DTOs.AutoAssign;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IAutoAssignmentPolicyRepository
    {
        Task<int?> GetUserFacultyIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<AutoAssignmentPolicyEditDto> GetOrCreateDefaultPolicyAsync(
            int facultyId,
            int actorUserId,
            CancellationToken cancellationToken = default);

        Task UpdateDefaultPolicyAsync(
            AutoAssignmentPolicyEditDto dto,
            int actorUserId,
            CancellationToken cancellationToken = default);
    }
}
