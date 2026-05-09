using ExamInvigilationManagement.Application.DTOs.Approval;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IExamScheduleApprovalRepository
    {
        Task<ApprovalUserContextDto?> GetUserContextAsync(int userId, CancellationToken cancellationToken = default);

        Task<ExamScheduleApprovalPageResultDto> GetIndexPageAsync(
            int facultyId,
            ExamScheduleApprovalSearchDto search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<List<ExamScheduleApprovalIndexItemDto>> GetBulkTargetsAsync(
            int facultyId,
            IEnumerable<int> examScheduleIds,
            CancellationToken cancellationToken = default);

        Task<List<int>> GetSecretaryRecipientIdsAsync(
            int facultyId,
            int excludeUserId,
            CancellationToken cancellationToken = default);

        Task SaveBulkAsync(
            ExamScheduleApprovalSavePlanDto plan,
            CancellationToken cancellationToken = default);
    }
}