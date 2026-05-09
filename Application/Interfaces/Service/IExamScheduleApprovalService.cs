using ExamInvigilationManagement.Application.DTOs.Approval;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IExamScheduleApprovalService
    {
        Task<ExamScheduleApprovalIndexPageDto> GetIndexAsync(
           ExamScheduleApprovalSearchDto search,
           int userId,
           int page = 1,
           int pageSize = 10,
           CancellationToken cancellationToken = default);

        Task<ExamScheduleApprovalBulkReviewResultDto> ReviewBulkAsync(
            ExamScheduleApprovalBulkReviewRequestDto request,
            int userId,
            CancellationToken cancellationToken = default);
    }
}