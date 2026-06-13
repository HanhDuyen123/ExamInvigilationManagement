using ExamInvigilationManagement.Application.DTOs.AutoAssign;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IAutoAssignmentService
    {
        Task<AutoAssignResultDto> AutoAssignAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default);

        Task<AutoAssignResultDto> PreviewAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default);

        Task<AutoAssignResultDto> SaveDraftAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default);

        Task<AutoAssignResultDto> CompareDraftAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default);

        Task<AutoAssignResultDto> ClearDraftAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
