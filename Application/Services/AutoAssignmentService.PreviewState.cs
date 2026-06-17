using ExamInvigilationManagement.Application.DTOs.AutoAssign;

namespace ExamInvigilationManagement.Application.Services
{
    public partial class AutoAssignmentService
    {
        private async Task<AutoAssignResultDto> SaveCachedPreviewAsync(AutoAssignRequestDto request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var key = BuildPreviewCacheKey(request.AssignerId, request.PreviewToken!);
            if (!_cache.TryGetValue<CachedPreviewPlan>(key, out var cached) || cached == null)
                throw new InvalidOperationException("Phương án xem trước đã hết hạn hoặc không còn hợp lệ. Vui lòng chạy lại trước khi lưu.");

            if (cached.AssignerId != request.AssignerId || cached.SemesterId != request.SemesterId || cached.PeriodId != request.PeriodId)
                throw new InvalidOperationException("Phương án xem trước không khớp với phạm vi phân công hiện tại. Vui lòng chạy lại.");

            await _repository.SavePlanAsync(cached.Plan, cancellationToken);
            _cache.Remove(key);

            var result = CloneResultForCache(cached.Result);
            result.IsPreview = false;
            result.PreviewToken = null;
            result.PlanSnapshot = null;
            result.Message = result.MissingSchedules > 0
                ? "Đã lưu đúng phương án đã xem trước, nhưng vẫn còn một số lịch thiếu giám thị."
                : "Đã lưu đúng phương án đã xem trước.";
            result.AssignerId = request.AssignerId;
            await AttachDraftStateAsync(result, includeComparison: false, cancellationToken);
            return result;
        }

        private Task<CachedPreviewPlan> GetCachedPreviewAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var key = BuildPreviewCacheKey(request.AssignerId, request.PreviewToken!);
            if (!_cache.TryGetValue<CachedPreviewPlan>(key, out var cached) || cached == null)
                throw new InvalidOperationException("Phương án xem trước đã hết hạn hoặc không còn hợp lệ. Vui lòng chạy xem trước lại trước khi thao tác.");

            if (cached.AssignerId != request.AssignerId || cached.SemesterId != request.SemesterId || cached.PeriodId != request.PeriodId)
                throw new InvalidOperationException("Phương án xem trước không khớp với phạm vi hiện tại. Vui lòng chạy xem trước lại.");

            return Task.FromResult(cached);
        }

        private void CacheDraftPlan(int assignerId, int semesterId, int periodId, CachedPreviewPlan source)
        {
            _cache.Set(
                BuildDraftCacheKey(assignerId, semesterId, periodId),
                new CachedPreviewPlan(assignerId, semesterId, periodId, source.Plan, CloneResultForCache(source.Result), DateTime.UtcNow),
                DraftCacheLifetime);
        }

        private Task AttachDraftStateAsync(
            AutoAssignResultDto currentResult,
            bool includeComparison,
            CancellationToken cancellationToken = default)
        {
            if (!currentResult.AssignerId.HasValue || !currentResult.SemesterId.HasValue || !currentResult.PeriodId.HasValue)
                return Task.CompletedTask;

            var draftKey = BuildDraftCacheKey(currentResult.AssignerId.Value, currentResult.SemesterId.Value, currentResult.PeriodId.Value);
            if (!_cache.TryGetValue<CachedPreviewPlan>(draftKey, out var draft) || draft == null)
            {
                currentResult.HasSavedDraft = false;
                currentResult.Comparison = null;
                return Task.CompletedTask;
            }

            currentResult.HasSavedDraft = true;
            currentResult.Comparison = includeComparison ? BuildComparison(currentResult, draft.Result) : null;
            return Task.CompletedTask;
        }

        private static AutoAssignComparisonDto BuildComparison(
            AutoAssignResultDto current,
            AutoAssignResultDto baseline)
        {
            var baselineByScheduleId = baseline.Details.ToDictionary(x => x.ExamScheduleId);
            var currentAssignmentSet = FlattenAssignments(current).ToHashSet();
            var baselineAssignmentSet = FlattenAssignments(baseline).ToHashSet();
            var changedDetails = current.Details
                .Where(x => baselineByScheduleId.ContainsKey(x.ExamScheduleId))
                .Select(x => BuildScheduleComparison(x, baselineByScheduleId[x.ExamScheduleId]))
                .Where(x => x.StatusBefore != x.StatusAfter || x.Positions.Any(p => p.Changed))
                .ToList();

            var changedSchedules = changedDetails.Count;
            var summary = changedSchedules > 0
                ? $"Có {changedSchedules} lịch thay đổi so với bản tạm."
                : "Không có thay đổi về giám thị hoặc trạng thái so với bản tạm.";

            return new AutoAssignComparisonDto
            {
                HasBaseline = true,
                BaselineAssignedInvigilators = baseline.AssignedInvigilators,
                BaselineFullyAssignedSchedules = baseline.FullyAssignedSchedules,
                BaselineMissingSchedules = baseline.MissingSchedules,
                ChangedSchedules = changedSchedules,
                AddedAssignments = currentAssignmentSet.Except(baselineAssignmentSet).Count(),
                RemovedAssignments = baselineAssignmentSet.Except(currentAssignmentSet).Count(),
                Summary = summary,
                ChangedDetails = changedDetails
            };
        }

        private static AutoAssignScheduleComparisonDto BuildScheduleComparison(
            AutoAssignScheduleResultDto current,
            AutoAssignScheduleResultDto baseline)
        {
            var currentByPosition = current.AssignedLecturers.ToDictionary(x => x.PositionNo);
            var baselineByPosition = baseline.AssignedLecturers.ToDictionary(x => x.PositionNo);
            var positions = currentByPosition.Keys
                .Concat(baselineByPosition.Keys)
                .DefaultIfEmpty((byte)1)
                .Distinct()
                .OrderBy(x => x)
                .Select(position =>
                {
                    currentByPosition.TryGetValue(position, out var currentLecturer);
                    baselineByPosition.TryGetValue(position, out var baselineLecturer);
                    var currentName = currentLecturer?.FullName ?? "Chưa gán";
                    var baselineName = baselineLecturer?.FullName ?? "Chưa gán";
                    return new AutoAssignPositionComparisonDto
                    {
                        PositionNo = position,
                        BaselineLecturerName = baselineName,
                        CurrentLecturerName = currentName,
                        Changed = !string.Equals(currentName, baselineName, StringComparison.OrdinalIgnoreCase)
                    };
                })
                .ToList();

            return new AutoAssignScheduleComparisonDto
            {
                ExamScheduleId = current.ExamScheduleId,
                ExamDate = current.ExamDate,
                SlotName = current.SlotName,
                RoomDisplay = current.RoomDisplay,
                SubjectName = current.SubjectName,
                ClassName = current.ClassName,
                StatusBefore = baseline.StatusAfter,
                StatusAfter = current.StatusAfter,
                Positions = positions
            };
        }

        private static List<(int ScheduleId, int LecturerId, byte PositionNo)> FlattenAssignments(AutoAssignResultDto result)
        {
            return result.Details
                .SelectMany(x => x.AssignedLecturers.Select(l => (x.ExamScheduleId, l.UserId, l.PositionNo)))
                .ToList();
        }

        private static string BuildPreviewCacheKey(int assignerId, string token)
            => $"{PreviewCachePrefix}:{assignerId}:{token}";

        private static string BuildDraftCacheKey(int assignerId, int semesterId, int periodId)
            => $"{DraftCachePrefix}:{assignerId}:{semesterId}:{periodId}";

        private static AutoAssignResultDto CloneResultForCache(AutoAssignResultDto source)
        {
            return new AutoAssignResultDto
            {
                Success = source.Success,
                Message = source.Message,
                TotalSchedules = source.TotalSchedules,
                AssignedInvigilators = source.AssignedInvigilators,
                FullyAssignedSchedules = source.FullyAssignedSchedules,
                MissingSchedules = source.MissingSchedules,
                IsPreview = source.IsPreview,
                IsOptimizationProven = source.IsOptimizationProven,
                HasSavedDraft = source.HasSavedDraft,
                DraftSaved = source.DraftSaved,
                DraftCleared = source.DraftCleared,
                AssignerId = source.AssignerId,
                SemesterId = source.SemesterId,
                PeriodId = source.PeriodId,
                PreviewToken = source.PreviewToken,
                Warnings = source.Warnings.ToList(),
                Comparison = source.Comparison == null
                    ? null
                    : new AutoAssignComparisonDto
                    {
                        HasBaseline = source.Comparison.HasBaseline,
                        BaselineAssignedInvigilators = source.Comparison.BaselineAssignedInvigilators,
                        BaselineFullyAssignedSchedules = source.Comparison.BaselineFullyAssignedSchedules,
                        BaselineMissingSchedules = source.Comparison.BaselineMissingSchedules,
                        ChangedSchedules = source.Comparison.ChangedSchedules,
                        AddedAssignments = source.Comparison.AddedAssignments,
                        RemovedAssignments = source.Comparison.RemovedAssignments,
                        Summary = source.Comparison.Summary,
                        ChangedDetails = source.Comparison.ChangedDetails.Select(d => new AutoAssignScheduleComparisonDto
                        {
                            ExamScheduleId = d.ExamScheduleId,
                            ExamDate = d.ExamDate,
                            SlotName = d.SlotName,
                            RoomDisplay = d.RoomDisplay,
                            SubjectName = d.SubjectName,
                            ClassName = d.ClassName,
                            StatusBefore = d.StatusBefore,
                            StatusAfter = d.StatusAfter,
                            Positions = d.Positions.Select(p => new AutoAssignPositionComparisonDto
                            {
                                PositionNo = p.PositionNo,
                                BaselineLecturerName = p.BaselineLecturerName,
                                CurrentLecturerName = p.CurrentLecturerName,
                                Changed = p.Changed
                            }).ToList()
                        }).ToList()
                    },
                Details = source.Details.Select(d => new AutoAssignScheduleResultDto
                {
                    ExamScheduleId = d.ExamScheduleId,
                    ExamDate = d.ExamDate,
                    SlotName = d.SlotName,
                    RoomDisplay = d.RoomDisplay,
                    SubjectName = d.SubjectName,
                    ClassName = d.ClassName,
                    ExamFormatDisplay = d.ExamFormatDisplay,
                    StatusBefore = d.StatusBefore,
                    StatusAfter = d.StatusAfter,
                    RequiredCount = d.RequiredCount,
                    AssignedCount = d.AssignedCount,
                    Message = d.Message,
                    AssignedLecturers = d.AssignedLecturers.Select(l => new AutoAssignAssignedLecturerDto
                    {
                        UserId = l.UserId,
                        UserName = l.UserName,
                        FullName = l.FullName,
                        PositionNo = l.PositionNo,
                        Score = l.Score,
                        Reason = l.Reason
                    }).ToList()
                }).ToList()
            };
        }
    }
}
