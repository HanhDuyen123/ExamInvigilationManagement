using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;

namespace ExamInvigilationManagement.Application.Services
{
    public partial class AutoAssignmentService : IAutoAssignmentService
    {
        private const int InternalSolverTimeLimitSeconds = 20;
        private const double MinimumSolverPhaseSeconds = 0.25;
        private const string PreviewCachePrefix = "AutoAssignmentPreview";
        private const string DraftCachePrefix = "AutoAssignmentDraft";
        private static readonly TimeSpan DraftCacheLifetime = TimeSpan.FromHours(24);

        private readonly IAutoAssignmentRepository _repository;
        private readonly ICacheService _cache;

        public AutoAssignmentService(IAutoAssignmentRepository repository, ICacheService cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<AutoAssignResultDto> AutoAssignAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            request.PreviewOnly = false;
            if (!string.IsNullOrWhiteSpace(request.PreviewToken))
                return await SaveCachedPreviewAsync(request, cancellationToken);

            return await BuildAssignmentAsync(request, cancellationToken);
        }

        public async Task<AutoAssignResultDto> PreviewAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            request.PreviewOnly = true;
            var result = await BuildAssignmentAsync(request, cancellationToken);
            if (result.Success && result.PlanSnapshot != null)
            {
                var token = Guid.NewGuid().ToString("N");
                result.PreviewToken = token;
                _cache.Set(
                    BuildPreviewCacheKey(request.AssignerId, token),
                    new CachedPreviewPlan(
                        request.AssignerId,
                        request.SemesterId!.Value,
                        request.PeriodId!.Value,
                        result.PlanSnapshot,
                        CloneResultForCache(result),
                        DateTime.UtcNow),
                    TimeSpan.FromMinutes(60));
            }

            await AttachDraftStateAsync(result, includeComparison: false, cancellationToken);
            result.PlanSnapshot = null;
            return result;
        }

        public async Task<AutoAssignResultDto> SaveDraftAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var cached = await GetCachedPreviewAsync(request, cancellationToken);
            CacheDraftPlan(request.AssignerId, request.SemesterId!.Value, request.PeriodId!.Value, cached);

            var result = CloneResultForCache(cached.Result);
            result.IsPreview = true;
            result.PreviewToken = cached.Result.PreviewToken;
            result.AssignerId = request.AssignerId;
            result.HasSavedDraft = true;
            result.DraftSaved = true;
            result.DraftCleared = false;
            result.Comparison = null;
            return result;
        }

        public async Task<AutoAssignResultDto> CompareDraftAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var cached = await GetCachedPreviewAsync(request, cancellationToken);
            var result = CloneResultForCache(cached.Result);
            result.IsPreview = true;
            result.PreviewToken = cached.Result.PreviewToken;
            result.AssignerId = request.AssignerId;
            await AttachDraftStateAsync(result, includeComparison: true, cancellationToken);

            if (result.Comparison == null)
                throw new InvalidOperationException("Chưa có bản tạm để so sánh. Vui lòng lưu bản tạm trước.");

            return result;
        }

        public async Task<AutoAssignResultDto> ClearDraftAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var cached = await GetCachedPreviewAsync(request, cancellationToken);
            _cache.Remove(BuildDraftCacheKey(request.AssignerId, request.SemesterId!.Value, request.PeriodId!.Value));

            var result = CloneResultForCache(cached.Result);
            result.IsPreview = true;
            result.PreviewToken = cached.Result.PreviewToken;
            result.AssignerId = request.AssignerId;
            result.HasSavedDraft = false;
            result.DraftSaved = false;
            result.DraftCleared = true;
            result.Comparison = null;
            return result;
        }
    }
}
