using ExamInvigilationManagement.Application.DTOs.ManualAssignment;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IManualAssignmentService
    {
        Task<ManualAssignmentPageDto> GetPageAsync(
            int scheduleId,
            int assignerId,
            CancellationToken cancellationToken = default);

        Task<ManualAssignmentResultDto> AssignAsync(
            ManualAssignmentRequestDto request,
            CancellationToken cancellationToken = default);
    }
}