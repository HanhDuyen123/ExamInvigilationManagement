using ExamInvigilationManagement.Application.DTOs.InvigilatorResponse;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IInvigilatorResponseService
    {
        Task<PagedResult<InvigilatorAssignmentItemDto>> GetAssignmentsAsync(
            int userId,
            InvigilatorAssignmentSearchDto search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<InvigilatorResponseResultDto> SubmitAsync(
            int userId,
            InvigilatorResponseSubmitDto request,
            CancellationToken cancellationToken = default);

        Task<InvigilatorConfirmationResultDto> SendConfirmationAsync(
            InvigilatorConfirmationRequestDto request,
            string confirmationUrl,
            CancellationToken cancellationToken = default);
    }
}
