using ExamInvigilationManagement.Application.DTOs.InvigilatorSubstitution;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IInvigilatorSubstitutionService
    {
        Task<InvigilatorSubstitutionCreatePageDto> GetCreatePageAsync(int examInvigilatorId, int userId, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionResultDto> CreateAsync(InvigilatorSubstitutionCreateRequestDto request, int userId, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionIndexPageDto> GetIndexAsync(int secretaryId, InvigilatorSubstitutionSearchDto search, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionDetailDto> GetDetailAsync(int substitutionId, int secretaryId, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionResultDto> ApproveAsync(int substitutionId, int secretaryId, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionResultDto> ApproveWithReplacementAsync(int substitutionId, int replacementUserId, int secretaryId, CancellationToken cancellationToken = default);
        Task<InvigilatorSubstitutionResultDto> RejectAsync(int substitutionId, int secretaryId, CancellationToken cancellationToken = default);
    }
}
