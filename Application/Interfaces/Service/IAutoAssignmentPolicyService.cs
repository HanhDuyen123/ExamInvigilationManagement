using ExamInvigilationManagement.Application.DTOs.AutoAssign;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IAutoAssignmentPolicyService
    {
        Task<AutoAssignmentPolicyEditDto> GetDefaultPolicyAsync(
            int actorUserId,
            CancellationToken cancellationToken = default);

        Task UpdateDefaultPolicyAsync(
            AutoAssignmentPolicyEditDto dto,
            int actorUserId,
            CancellationToken cancellationToken = default);
    }
}
