using System.Diagnostics;
using System.Globalization;
using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Constants;
using Google.OrTools.Sat;
using Microsoft.Extensions.Caching.Memory;

namespace ExamInvigilationManagement.Application.Services
{
    public class AutoAssignmentService : IAutoAssignmentService
    {
        private const int InternalSolverTimeLimitSeconds = 20;
        private const double MinimumSolverPhaseSeconds = 0.25;
        private const string PreviewCachePrefix = "AutoAssignmentPreview";
        private const string DraftCachePrefix = "AutoAssignmentDraft";
        private static readonly TimeSpan DraftCacheLifetime = TimeSpan.FromHours(24);

        private readonly IAutoAssignmentRepository _repository;
        private readonly IMemoryCache _memoryCache;

        public AutoAssignmentService(IAutoAssignmentRepository repository, IMemoryCache memoryCache)
        {
            _repository = repository;
            _memoryCache = memoryCache;
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
                _memoryCache.Set(
                    BuildPreviewCacheKey(request.AssignerId, token),
                    new CachedPreviewPlan(
                        request.AssignerId,
                        request.SemesterId!.Value,
                        request.PeriodId!.Value,
                        result.PlanSnapshot,
                        CloneResultForCache(result),
                        DateTime.UtcNow),
                    TimeSpan.FromMinutes(20));
            }

            await AttachDraftStateAsync(result, includeComparison: false, cancellationToken);
            result.PlanSnapshot = null;
            return result;
        }

        private async Task<AutoAssignResultDto> SaveCachedPreviewAsync(AutoAssignRequestDto request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var key = BuildPreviewCacheKey(request.AssignerId, request.PreviewToken!);
            if (!_memoryCache.TryGetValue<CachedPreviewPlan>(key, out var cached) || cached == null)
                throw new InvalidOperationException("Phương án xem trước đã hết hạn hoặc không còn hợp lệ. Vui lòng chạy xem trước lại trước khi lưu.");

            if (cached.AssignerId != request.AssignerId || cached.SemesterId != request.SemesterId || cached.PeriodId != request.PeriodId)
                throw new InvalidOperationException("Phương án xem trước không khớp với phạm vi phân công hiện tại. Vui lòng chạy xem trước lại.");

            await _repository.SavePlanAsync(cached.Plan, cancellationToken);
            _memoryCache.Remove(key);

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
            _memoryCache.Remove(BuildDraftCacheKey(request.AssignerId, request.SemesterId!.Value, request.PeriodId!.Value));

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

        private Task<CachedPreviewPlan> GetCachedPreviewAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var key = BuildPreviewCacheKey(request.AssignerId, request.PreviewToken!);
            if (!_memoryCache.TryGetValue<CachedPreviewPlan>(key, out var cached) || cached == null)
                throw new InvalidOperationException("Phương án xem trước đã hết hạn hoặc không còn hợp lệ. Vui lòng chạy xem trước lại trước khi thao tác.");

            if (cached.AssignerId != request.AssignerId || cached.SemesterId != request.SemesterId || cached.PeriodId != request.PeriodId)
                throw new InvalidOperationException("Phương án xem trước không khớp với phạm vi hiện tại. Vui lòng chạy xem trước lại.");

            return Task.FromResult(cached);
        }

        private void CacheDraftPlan(int assignerId, int semesterId, int periodId, CachedPreviewPlan source)
        {
            _memoryCache.Set(
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
            if (!_memoryCache.TryGetValue<CachedPreviewPlan>(draftKey, out var draft) || draft == null)
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

        private async Task<AutoAssignResultDto> BuildAssignmentAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var facultyId = await _repository.GetUserFacultyIdAsync(request.AssignerId, cancellationToken);
            if (facultyId is null || facultyId <= 0)
                throw new ArgumentException("Không xác định được khoa của người thực hiện auto assignment.");

            var schedules = (await _repository.GetSchedulesAsync(
                    request.SemesterId!.Value,
                    request.PeriodId!.Value,
                    facultyId.Value,
                    cancellationToken))
                .ToList();

            var policy = await _repository.GetEffectivePolicyAsync(
                facultyId.Value,
                request.SemesterId.Value,
                request.PeriodId.Value,
                cancellationToken);

            var lecturers = (await _repository.GetActiveLecturersAsync(
                    facultyId.Value,
                    schedules.Select(x => x.SubjectId),
                    schedules.Select(x => x.OfferingUserId),
                    cancellationToken))
                .ToList();

            var whitelistedLecturerIds = await _repository.GetPeriodAvailableLecturerIdsAsync(
                request.PeriodId!.Value,
                facultyId.Value,
                lecturers.Select(x => x.UserId),
                cancellationToken);

            if (policy.RequirePeriodAvailabilityIfExists &&
                (whitelistedLecturerIds.Count > 0 || await _repository.HasPeriodAvailabilityListAsync(request.PeriodId.Value, facultyId.Value, cancellationToken)))
                lecturers = lecturers.Where(x => whitelistedLecturerIds.Contains(x.UserId)).ToList();

            var busyWholePeriodLecturerIds = await _repository.GetApprovedBusyPeriodLecturerIdsAsync(
                request.PeriodId!.Value,
                lecturers.Select(x => x.UserId),
                cancellationToken);

            if (busyWholePeriodLecturerIds.Count > 0)
                lecturers = lecturers.Where(x => !busyWholePeriodLecturerIds.Contains(x.UserId)).ToList();

            if (lecturers.Count == 0)
                throw new InvalidOperationException("Không có giảng viên hợp lệ để phân công.");

            var result = new AutoAssignResultDto
            {
                TotalSchedules = schedules.Count,
                IsPreview = request.PreviewOnly,
                AssignerId = request.AssignerId,
                SemesterId = request.SemesterId,
                PeriodId = request.PeriodId
            };

            if (schedules.Count == 0)
            {
                result.Success = true;
                result.Message = "Không có lịch thi thuộc khoa và kỳ/đợt đã chọn.";
                return result;
            }

            var scheduleIds = schedules.Select(x => x.ExamScheduleId).ToList();
            var slotIds = schedules.Select(x => x.SlotId).Distinct().ToList();
            var examDates = schedules
                .Select(x => DateOnly.FromDateTime(x.ExamDate))
                .Distinct()
                .ToList();

            var busySlots = await _repository.GetBusySlotsAsync(
                lecturers.Select(x => x.UserId),
                slotIds,
                examDates,
                cancellationToken);

            var existingAssignments = await _repository.GetExistingAssignmentsAsync(
                scheduleIds,
                cancellationToken);

            var lecturerLoads = await _repository.GetLecturerLoadsAsync(
                request.SemesterId!.Value,
                lecturers.Select(x => x.UserId),
                cancellationToken);

            var subjectLecturerMap = await _repository.GetSubjectLecturerMapAsync(
                schedules.Select(x => x.SubjectId),
                cancellationToken);
            var isLecturerRoleByUser = lecturers.ToDictionary(x => x.UserId, x => x.IsLecturerRole);

            foreach (var lecturer in lecturers)
            {
                if (!lecturerLoads.ContainsKey(lecturer.UserId))
                    lecturerLoads[lecturer.UserId] = 0;
            }

            var busyKeySet = busySlots
                .Select(x => (x.UserId, x.SlotId, x.BusyDate))
                .ToHashSet();

            var activeExistingAssignments = existingAssignments
                .Where(x => !x.IsRejected)
                .ToList();
            var assignmentMode = existingAssignments.Any(x => x.IsRejected)
                ? AutoAssignmentMode.RepairRejected
                : AutoAssignmentMode.InitialAssignment;

            var occupiedKeySet = new HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)>();
            foreach (var x in activeExistingAssignments)
                occupiedKeySet.Add((x.PersonKey, x.SlotId, DateOnly.FromDateTime(x.ExamDate)));

            var scheduleAssignedUsers = activeExistingAssignments
                .GroupBy(x => x.ExamScheduleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.PersonKey).ToHashSet());

            var scheduleAssignedPositions = activeExistingAssignments
                .GroupBy(x => x.ExamScheduleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.PositionNo).ToHashSet());

            var sameDayLoadMap = activeExistingAssignments
                .GroupBy(x => (x.PersonKey, DateOnly.FromDateTime(x.ExamDate)))
                .ToDictionary(g => g.Key, g => g.Count());

            var scheduleByIdForLocation = schedules.ToDictionary(x => x.ExamScheduleId);
            var sameDayLocationMap = activeExistingAssignments
                .Where(x => scheduleByIdForLocation.ContainsKey(x.ExamScheduleId))
                .GroupBy(x => (x.PersonKey, DateOnly.FromDateTime(x.ExamDate), scheduleByIdForLocation[x.ExamScheduleId].SessionId))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => scheduleByIdForLocation[x.ExamScheduleId]).ToList());

            var ownerScheduleCountByLecturer = schedules
                .GroupBy(x => x.OfferingUserPersonKey)
                .ToDictionary(g => g.Key, g => g.Count());

            var solverResult = TryBuildCpSatPlan(
                request,
                schedules,
                lecturers,
                lecturerLoads,
                busyKeySet,
                occupiedKeySet,
                scheduleAssignedUsers,
                scheduleAssignedPositions,
                sameDayLoadMap,
                sameDayLocationMap,
                subjectLecturerMap,
                isLecturerRoleByUser,
                assignmentMode,
                policy);

            if (solverResult != null)
            {
                solverResult.Result.IsPreview = request.PreviewOnly;
                solverResult.Result.SemesterId = request.SemesterId;
                solverResult.Result.PeriodId = request.PeriodId;
                if (!request.PreviewOnly && solverResult.Result.Success)
                    await _repository.SavePlanAsync(solverResult.Plan, cancellationToken);
                else if (request.PreviewOnly && solverResult.Result.Success)
                {
                    solverResult.Result.PlanSnapshot = solverResult.Plan;
                    solverResult.Result.Message = BuildPreviewMessage(solverResult.Result);
                }
                return solverResult.Result;
            }

            var orderedSchedules = schedules
                .Select(schedule =>
                {
                    var difficulty = CalculateScheduleDifficulty(
                        schedule,
                        lecturers,
                        scheduleAssignedUsers,
                        busyKeySet,
                        occupiedKeySet);

                    return new
                    {
                        Schedule = schedule,
                        Difficulty = difficulty
                    };
                })
                .OrderBy(x => x.Difficulty.TotalAvailableCandidates)
                .ThenByDescending(x => x.Difficulty.HasExactOwner)
                .ThenBy(x => x.Schedule.ExamDate)
                .ThenBy(x => x.Schedule.TimeStart)
                .ThenBy(x => x.Schedule.RoomDisplay)
                .ToList();

            var plan = new AutoAssignPlanDto();
            var detailByScheduleId = new Dictionary<int, AutoAssignScheduleResultDto>();

            foreach (var item in orderedSchedules)
            {
                var schedule = item.Schedule;

                var assignedUsers = scheduleAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var set)
                    ? set
                    : new HashSet<int>();
                var assignedPositions = scheduleAssignedPositions.TryGetValue(schedule.ExamScheduleId, out var positionSet)
                    ? positionSet
                    : new HashSet<byte>();

                scheduleAssignedUsers[schedule.ExamScheduleId] = assignedUsers;
                scheduleAssignedPositions[schedule.ExamScheduleId] = assignedPositions;

                detailByScheduleId[schedule.ExamScheduleId] = new AutoAssignScheduleResultDto
                {
                    ExamScheduleId = schedule.ExamScheduleId,
                    ExamDate = schedule.ExamDate,
                    SlotName = schedule.SlotName,
                    RoomDisplay = schedule.RoomDisplay,
                    SubjectName = schedule.SubjectName,
                    ClassName = schedule.ClassName,
                    ExamFormatDisplay = schedule.ExamFormatDisplay,
                    StatusBefore = schedule.Status,
                    RequiredCount = GetRequiredInvigilators(schedule, policy),
                    AssignedCount = assignedUsers.Count
                };
            }

            // PHASE 1: reserve exact owner cho từng lịch nếu khả dụng
            if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.ExactOwner) || orderedSchedules.Any(x => IsOwnerOnly(x.Schedule, policy))) foreach (var item in orderedSchedules)
            {
                var schedule = item.Schedule;
                if (IsSkippedByFormat(schedule, policy))
                    continue;

                if (IsFinalStatus(schedule.Status))
                {
                    detailByScheduleId[schedule.ExamScheduleId] = CreateSkippedDetail(schedule, GetRequiredInvigilators(schedule, policy));
                    continue;
                }

                var assignedUsers = scheduleAssignedUsers[schedule.ExamScheduleId];
                var assignedPositions = scheduleAssignedPositions[schedule.ExamScheduleId];
                var detail = detailByScheduleId[schedule.ExamScheduleId];
                var day = DateOnly.FromDateTime(schedule.ExamDate);

                var exactOwner = lecturers.FirstOrDefault(x =>
                    x.PersonKey == schedule.OfferingUserPersonKey &&
                    IsFeasibleExactOwner(x, schedule, assignedUsers, busyKeySet, occupiedKeySet) &&
                    IsWithinPolicyLoad(x, policy, lecturerLoads, sameDayLoadMap, day));

                if (exactOwner != null)
                {
                    var score = CalculateExactOwnerScore(
                        exactOwner,
                        schedule,
                        lecturerLoads,
                        sameDayLoadMap,
                        sameDayLocationMap,
                        day,
                        policy);

                    AssignOne(
                        plan,
                        detail,
                        schedule,
                        exactOwner,
                        request.AssignerId,
                        assignedUsers,
                        assignedPositions,
                        lecturerLoads,
                        sameDayLoadMap,
                        sameDayLocationMap,
                        occupiedKeySet,
                        GetRequiredInvigilators(schedule, policy),
                        score,
                        "Đang dạy lớp học phần");
                }
            }

            // PHASE 2: fill các vị trí còn thiếu bằng các giảng viên cùng khoa khác
            foreach (var item in orderedSchedules)
            {
                var schedule = item.Schedule;

                if (IsFinalStatus(schedule.Status))
                    continue;

                var assignedUsers = scheduleAssignedUsers[schedule.ExamScheduleId];
                var assignedPositions = scheduleAssignedPositions[schedule.ExamScheduleId];
                var detail = detailByScheduleId[schedule.ExamScheduleId];
                var day = DateOnly.FromDateTime(schedule.ExamDate);

                if (IsOwnerOnly(schedule, policy))
                {
                    detail.Message = "Hình thức thi được cấu hình chỉ giữ/gán giảng viên owner.";
                    if (assignedUsers.Count >= GetRequiredInvigilators(schedule, policy))
                        continue;
                }

                var requiredCount = GetRequiredInvigilators(schedule, policy);
                var need = Math.Max(0, requiredCount - assignedUsers.Count);
                if (IsOwnerOnly(schedule, policy))
                    need = 0;

                while (need > 0)
                {
                    var fallback = PickBestFallbackCandidate(
                        schedule,
                        lecturers,
                        assignedUsers,
                        lecturerLoads,
                        sameDayLoadMap,
                        sameDayLocationMap,
                        busyKeySet,
                        occupiedKeySet,
                        ownerScheduleCountByLecturer,
                        subjectLecturerMap,
                        isLecturerRoleByUser,
                        policy,
                        day,
                        request.RunSeed.GetValueOrDefault(1));

                    if (fallback == null)
                        break;

                    AssignOne(
                        plan,
                        detail,
                        schedule,
                        fallback.Lecturer,
                        request.AssignerId,
                        assignedUsers,
                        assignedPositions,
                        lecturerLoads,
                        sameDayLoadMap,
                        sameDayLocationMap,
                        occupiedKeySet,
                        requiredCount,
                        fallback.Score,
                        fallback.Reason);

                    need--;
                }

                var finalCount = assignedUsers.Count;
                var finalStatus = requiredCount == 0
                    ? schedule.Status
                    : finalCount >= requiredCount
                    ? "Chờ duyệt"
                    : "Thiếu giám thị";

                plan.ScheduleStatuses.Add(new AutoAssignScheduleStatusUpdateDto
                {
                    ExamScheduleId = schedule.ExamScheduleId,
                    Status = finalStatus
                });

                detail.AssignedCount = finalCount;
                detail.StatusAfter = finalStatus;
                detail.Message = requiredCount == 0
                    ? "Bỏ qua phân công theo cấu hình hình thức thi."
                    : finalCount >= requiredCount
                    ? $"Đã phân công đủ {requiredCount} giám thị."
                    : $"Thiếu {requiredCount - finalCount} giám thị.";
            }

            foreach (var item in orderedSchedules)
            {
                result.Details.Add(detailByScheduleId[item.Schedule.ExamScheduleId]);
            }

            if (!request.PreviewOnly)
                await _repository.SavePlanAsync(plan, cancellationToken);

            result.AssignedInvigilators = plan.NewInvigilators.Count;
            result.FullyAssignedSchedules = result.Details.Count(x => x.StatusAfter == "Chờ duyệt");
            result.MissingSchedules = result.Details.Count(x => x.StatusAfter == "Thiếu giám thị");
            result.Success = true;
            result.Message = request.PreviewOnly
                ? BuildPreviewMessage(result)
                : result.MissingSchedules > 0
                ? "Tự động phân công hoàn thành nhưng còn một số lịch thiếu giám thị."
                : "Tự động phân công hoàn thành.";
            if (request.PreviewOnly)
                result.PlanSnapshot = plan;

            if (result.MissingSchedules > 0)
                result.Warnings.Add("Một số lịch không đủ 2 giám thị do không còn người phù hợp theo lịch bận, trùng ca hoặc dữ liệu chuyên môn hiện có.");

            await AttachDraftStateAsync(result, includeComparison: false, cancellationToken);
            return result;
        }

        private static string BuildPreviewMessage(AutoAssignResultDto result)
        {
            return result.MissingSchedules > 0
                ? "Đây là bản xem trước. Nếu lưu, hệ thống sẽ phân công nhưng vẫn còn một số lịch thiếu giám thị."
                : "Đây là bản xem trước. Nếu lưu, hệ thống sẽ phân công đủ các lịch đủ điều kiện.";
        }

        private static void ValidateRequest(AutoAssignRequestDto request)
        {
            if (!request.SemesterId.HasValue || request.SemesterId.Value <= 0)
                throw new ArgumentException("Vui lòng chọn học kỳ.");

            if (!request.PeriodId.HasValue || request.PeriodId.Value <= 0)
                throw new ArgumentException("Vui lòng chọn đợt thi.");

            if (request.AssignerId <= 0)
                throw new ArgumentException("AssignerId không hợp lệ.");
        }

        private static AutoAssignScheduleResultDto CreateSkippedDetail(AutoAssignScheduleDto schedule, int requiredInvigilators)
        {
            return new AutoAssignScheduleResultDto
            {
                ExamScheduleId = schedule.ExamScheduleId,
                ExamDate = schedule.ExamDate,
                SlotName = schedule.SlotName,
                RoomDisplay = schedule.RoomDisplay,
                SubjectName = schedule.SubjectName,
                ClassName = schedule.ClassName,
                ExamFormatDisplay = schedule.ExamFormatDisplay,
                StatusBefore = schedule.Status,
                StatusAfter = schedule.Status,
                RequiredCount = requiredInvigilators,
                AssignedCount = 0,
                Message = "Không gán mới."
            };
        }

        private static (int TotalAvailableCandidates, bool HasExactOwner) CalculateScheduleDifficulty(
            AutoAssignScheduleDto schedule,
            List<AutoAssignLecturerDto> lecturers,
            Dictionary<int, HashSet<int>> scheduleAssignedUsers,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);

            if (!scheduleAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers))
                assignedUsers = new HashSet<int>();

            var hasExactOwner = lecturers.Any(l =>
                l.PersonKey == schedule.OfferingUserPersonKey &&
                IsFeasibleExactOwner(l, schedule, assignedUsers, busyKeySet, occupiedKeySet));

            var fallbackCandidates = lecturers.Count(l =>
                l.IsActive &&
                l.PersonKey != schedule.OfferingUserPersonKey &&
                !assignedUsers.Contains(l.PersonKey) &&
                !busyKeySet.Contains((l.UserId, schedule.SlotId, day)) &&
                !occupiedKeySet.Contains((l.PersonKey, schedule.SlotId, day)));

            return (fallbackCandidates + (hasExactOwner ? 1 : 0), hasExactOwner);
        }

        private static bool IsFeasibleExactOwner(
            AutoAssignLecturerDto lecturer,
            AutoAssignScheduleDto schedule,
            HashSet<int> assignedUsers,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var busyKey = (lecturer.UserId, schedule.SlotId, day);
            var occupiedKey = (lecturer.PersonKey, schedule.SlotId, day);

            return lecturer.IsActive
                   && lecturer.PersonKey == schedule.OfferingUserPersonKey
                   && !assignedUsers.Contains(lecturer.PersonKey)
                   && !busyKeySet.Contains(busyKey)
                   && !occupiedKeySet.Contains(occupiedKey);
        }

        private static FallbackCandidate? PickBestFallbackCandidate(
            AutoAssignScheduleDto schedule,
            List<AutoAssignLecturerDto> lecturers,
            HashSet<int> assignedUsers,
            Dictionary<int, int> lecturerLoads,
            Dictionary<(int PersonKey, DateOnly Day), int> sameDayLoadMap,
            Dictionary<(int PersonKey, DateOnly Day, int SessionId), List<AutoAssignScheduleDto>> sameDayLocationMap,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            Dictionary<int, int> ownerScheduleCountByLecturer,
            Dictionary<string, HashSet<int>> subjectLecturerMap,
            Dictionary<int, bool> isLecturerRoleByUser,
            AutoAssignmentPolicyDto policy,
            DateOnly day,
            int runSeed)
        {
            var candidates = lecturers
                .Where(l =>
                {
                    if (!IsFeasibleFallback(l, schedule, assignedUsers, busyKeySet, occupiedKeySet) ||
                        !IsWithinPolicyLoad(l, policy, lecturerLoads, sameDayLoadMap, day))
                        return false;

                    var tier = GetCandidateTier(l, schedule, subjectLecturerMap, isLecturerRoleByUser);
                    return tier != CandidateTier.FacultyMember || policy.AllowFacultyMemberAsFallback;
                })
                .Select(l =>
                {
                    var load = lecturerLoads.TryGetValue(l.UserId, out var currentLoad) ? currentLoad : 0;
                    var sameDayLoad = sameDayLoadMap.TryGetValue((l.PersonKey, day), out var d) ? d : 0;
                    var ownerCount = ownerScheduleCountByLecturer.TryGetValue(l.PersonKey, out var c) ? c : 0;
                    var tier = GetCandidateTier(l, schedule, subjectLecturerMap, isLecturerRoleByUser);
                    var locationCost = CalculateSameDayLocationCost(l.PersonKey, day, schedule, sameDayLocationMap);

                    var score = 0;
                    var reasons = new List<string>();

                    var specialtyPriority = GetExamFormatPriority(schedule.ExamFormatDisplay);
                    if (tier == CandidateTier.SameSubject)
                    {
                        if (specialtyPriority == ExamFormatPriority.Oral && policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.OralSpecialist))
                        {
                            score += policy.GetWeight(AutoAssignmentPolicyRuleCodes.OralSpecialist, 9_500);
                            reasons.Add("Ưu tiên chuyên môn cho thi vấn đáp");
                        }
                        else if (specialtyPriority == ExamFormatPriority.Practical && policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.PracticalSpecialist))
                        {
                            score += policy.GetWeight(AutoAssignmentPolicyRuleCodes.PracticalSpecialist, 7_500);
                            reasons.Add("Ưu tiên chuyên môn cho thi thực hành");
                        }
                        else if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.SameSubject))
                        {
                            score += policy.GetWeight(AutoAssignmentPolicyRuleCodes.SameSubject, 2_500);
                            reasons.Add("Có chuyên môn môn thi");
                        }
                    }
                    else if (tier == CandidateTier.ExactOwner)
                    {
                        if (specialtyPriority == ExamFormatPriority.Oral && policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.OralSpecialist))
                        {
                            score += policy.GetWeight(AutoAssignmentPolicyRuleCodes.OralSpecialist, 11_000);
                            reasons.Add("Ưu tiên chuyên môn cho thi vấn đáp");
                        }
                        else if (specialtyPriority == ExamFormatPriority.Practical && policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.PracticalSpecialist))
                        {
                            score += policy.GetWeight(AutoAssignmentPolicyRuleCodes.PracticalSpecialist, 9_000);
                            reasons.Add("Ưu tiên chuyên môn cho thi thực hành");
                        }
                        else if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.ExactOwner))
                        {
                            score += policy.GetWeight(AutoAssignmentPolicyRuleCodes.ExactOwner, 5_000);
                            reasons.Add("Đang dạy lớp học phần");
                        }
                    }
                    else if (tier == CandidateTier.FacultyMember)
                    {
                        score -= policy.GetWeight(AutoAssignmentPolicyRuleCodes.FacultyMember, 6_500);
                        if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.FacultyMember))
                            reasons.Add($"Dự phòng từ role {l.RoleName}");
                    }
                    else
                    {
                        score -= policy.GetWeight(AutoAssignmentPolicyRuleCodes.Emergency, 2_500);
                        if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Emergency))
                            reasons.Add("Phù hợp lịch");
                    }

                    if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.LowLoad))
                        score += Math.Max(0, 1000 - load * 120);

                    if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.SameDayLoad))
                        score += Math.Max(0, 120 - sameDayLoad * 40);

                    if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Location))
                    {
                        score += GetLocationScoreBonus(locationCost);
                        var locationReason = GetLocationReason(locationCost);
                        if (!string.IsNullOrWhiteSpace(locationReason))
                            reasons.Add(locationReason);
                    }

                    if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.OwnerReservePenalty))
                        score -= ownerCount * policy.GetWeight(AutoAssignmentPolicyRuleCodes.OwnerReservePenalty, 150);

                    return new FallbackCandidate(
                        Lecturer: l,
                        Score: score,
                        TieBreaker: StableJitter(runSeed, schedule.ExamScheduleId, l.UserId),
                        Reason: reasons.Count > 0 ? string.Join("; ", reasons) : "Đáp ứng ràng buộc bắt buộc");
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.TieBreaker)
                .ThenBy(x => x.Lecturer.UserName)
                .ToList();

            return candidates.FirstOrDefault();
        }

        private static bool IsFeasibleFallback(
            AutoAssignLecturerDto lecturer,
            AutoAssignScheduleDto schedule,
            HashSet<int> assignedUsers,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var busyKey = (lecturer.UserId, schedule.SlotId, day);
            var occupiedKey = (lecturer.PersonKey, schedule.SlotId, day);

            // Chỉ cấm chính owner của lịch hiện tại.
            // Không cấm toàn bộ lecturer đang là owner của lịch khác,
            // vì pha 1 đã reserve owner rồi.
            return lecturer.IsActive
                   && lecturer.PersonKey != schedule.OfferingUserPersonKey
                   && !assignedUsers.Contains(lecturer.PersonKey)
                   && !busyKeySet.Contains(busyKey)
                   && !occupiedKeySet.Contains(occupiedKey);
        }

        private static int CalculateExactOwnerScore(
            AutoAssignLecturerDto lecturer,
            AutoAssignScheduleDto schedule,
            Dictionary<int, int> lecturerLoads,
            Dictionary<(int PersonKey, DateOnly Day), int> sameDayLoadMap,
            Dictionary<(int PersonKey, DateOnly Day, int SessionId), List<AutoAssignScheduleDto>> sameDayLocationMap,
            DateOnly day,
            AutoAssignmentPolicyDto policy)
        {
            var load = lecturerLoads.TryGetValue(lecturer.UserId, out var currentLoad) ? currentLoad : 0;
            var sameDayLoad = sameDayLoadMap.TryGetValue((lecturer.PersonKey, day), out var d) ? d : 0;
            var locationCost = CalculateSameDayLocationCost(lecturer.PersonKey, day, schedule, sameDayLocationMap);

            var score = policy.GetWeight(AutoAssignmentPolicyRuleCodes.ExactOwner, 5_000);
            if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.LowLoad))
                score += Math.Max(0, 500 - load * 100);
            if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.SameDayLoad))
                score += Math.Max(0, 100 - sameDayLoad * 30);
            if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Location))
                score += GetLocationScoreBonus(locationCost);

            return score;
        }

        private static bool IsWithinPolicyLoad(
            AutoAssignLecturerDto lecturer,
            AutoAssignmentPolicyDto policy,
            Dictionary<int, int> lecturerLoads,
            Dictionary<(int PersonKey, DateOnly Day), int> sameDayLoadMap,
            DateOnly day)
        {
            var currentLoad = lecturerLoads.TryGetValue(lecturer.UserId, out var load) ? load : 0;
            if (policy.MaxAssignmentsPerPeriod.HasValue && currentLoad >= policy.MaxAssignmentsPerPeriod.Value)
                return false;

            var sameDayLoad = sameDayLoadMap.TryGetValue((lecturer.PersonKey, day), out var dayLoad) ? dayLoad : 0;
            return !policy.MaxAssignmentsPerDay.HasValue || sameDayLoad < policy.MaxAssignmentsPerDay.Value;
        }

        private static void AssignOne(
            AutoAssignPlanDto plan,
            AutoAssignScheduleResultDto detail,
            AutoAssignScheduleDto schedule,
            AutoAssignLecturerDto lecturer,
            int assignerId,
            HashSet<int> assignedUsers,
            HashSet<byte> assignedPositions,
            Dictionary<int, int> lecturerLoads,
            Dictionary<(int PersonKey, DateOnly Day), int> sameDayLoadMap,
            Dictionary<(int PersonKey, DateOnly Day, int SessionId), List<AutoAssignScheduleDto>> sameDayLocationMap,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            int requiredInvigilators,
            int score,
            string reason)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var positionNo = GetNextPositionNo(assignedPositions, requiredInvigilators);

            plan.NewInvigilators.Add(new AutoAssignInvigilatorCreateDto
            {
                AssigneeId = lecturer.UserId,
                AssignerId = assignerId,
                ExamScheduleId = schedule.ExamScheduleId,
                PositionNo = positionNo,
                Status = ExamInvigilatorStatuses.PendingConfirmation,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });

            assignedUsers.Add(lecturer.PersonKey);
            assignedPositions.Add(positionNo);
            occupiedKeySet.Add((lecturer.PersonKey, schedule.SlotId, day));

            lecturerLoads[lecturer.UserId] = lecturerLoads.TryGetValue(lecturer.UserId, out var load)
                ? load + 1
                : 1;

            var sameDayKey = (lecturer.PersonKey, day);
            sameDayLoadMap[sameDayKey] = sameDayLoadMap.TryGetValue(sameDayKey, out var dayLoad)
                ? dayLoad + 1
                : 1;

            var sameSessionKey = (lecturer.PersonKey, day, schedule.SessionId);
            if (!sameDayLocationMap.TryGetValue(sameSessionKey, out var sameDayLocations))
            {
                sameDayLocations = new List<AutoAssignScheduleDto>();
                sameDayLocationMap[sameSessionKey] = sameDayLocations;
            }

            sameDayLocations.Add(schedule);

            detail.AssignedLecturers.Add(new AutoAssignAssignedLecturerDto
            {
                UserId = lecturer.UserId,
                UserName = lecturer.UserName,
                FullName = lecturer.FullName,
                PositionNo = positionNo,
                Score = score,
                Reason = reason
            });
        }

        private static byte GetNextPositionNo(HashSet<byte> assignedPositions, int requiredInvigilators)
        {
            for (byte position = 1; position <= requiredInvigilators; position++)
            {
                if (!assignedPositions.Contains(position))
                    return position;
            }

            return (byte)(assignedPositions.Count + 1);
        }

        private static CpSatAssignmentResult? TryBuildCpSatPlan(
            AutoAssignRequestDto request,
            List<AutoAssignScheduleDto> schedules,
            List<AutoAssignLecturerDto> lecturers,
            Dictionary<int, int> lecturerLoads,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            Dictionary<int, HashSet<int>> scheduleAssignedUsers,
            Dictionary<int, HashSet<byte>> scheduleAssignedPositions,
            Dictionary<(int PersonKey, DateOnly Day), int> sameDayLoadMap,
            Dictionary<(int PersonKey, DateOnly Day, int SessionId), List<AutoAssignScheduleDto>> sameDayLocationMap,
            Dictionary<string, HashSet<int>> subjectLecturerMap,
            Dictionary<int, bool> isLecturerRoleByUser,
            AutoAssignmentMode assignmentMode,
            AutoAssignmentPolicyDto policy)
        {
            try
            {
                var model = new CpModel();
                var variables = new Dictionary<(int ScheduleId, int LecturerId), BoolVar>();
                var fairnessTerms = new List<LinearExpr>();
                var shortageVars = new List<IntVar>();
                var exactVars = new List<BoolVar>();
                var sameSubjectVars = new List<BoolVar>();
                var oralSpecialistVars = new List<BoolVar>();
                var practicalSpecialistVars = new List<BoolVar>();
                var emergencyVars = new List<BoolVar>();
                var facultyMemberVars = new List<BoolVar>();
                var locationCostTerms = new List<LinearExpr>();
                var scheduleById = schedules.ToDictionary(x => x.ExamScheduleId);
                var lecturerById = lecturers.ToDictionary(x => x.UserId);

                var processableSchedules = schedules
                    .Where(x => CanProcessSchedule(x, scheduleAssignedUsers, GetRequiredInvigilators(x, policy)))
                    .ToList();
                var processableScheduleIds = processableSchedules.Select(x => x.ExamScheduleId).ToHashSet();

                foreach (var schedule in processableSchedules)
                {
                    var assignedUsers = scheduleAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var set)
                        ? set
                        : new HashSet<int>();
                    var requiredCount = GetRequiredInvigilators(schedule, policy);
                    var need = Math.Max(0, requiredCount - assignedUsers.Count);
                    if (need == 0)
                    {
                        continue;
                    }

                    var day = DateOnly.FromDateTime(schedule.ExamDate);
                    var scheduleVars = new List<BoolVar>();

                    foreach (var lecturer in lecturers.Where(x => IsFeasibleCpSatCandidate(
                        x,
                        schedule,
                        assignedUsers,
                        lecturerLoads,
                        sameDayLoadMap,
                        busyKeySet,
                        occupiedKeySet,
                        policy)))
                    {
                        var tier = GetCandidateTier(lecturer, schedule, subjectLecturerMap, isLecturerRoleByUser);
                        if (IsOwnerOnly(schedule, policy) && lecturer.PersonKey != schedule.OfferingUserPersonKey)
                            continue;
                        if (tier == CandidateTier.FacultyMember && !policy.AllowFacultyMemberAsFallback)
                            continue;

                        var variable = model.NewBoolVar($"x_s{schedule.ExamScheduleId}_u{lecturer.UserId}");
                        variables[(schedule.ExamScheduleId, lecturer.UserId)] = variable;
                        scheduleVars.Add(variable);

                        if (tier == CandidateTier.ExactOwner && policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.ExactOwner))
                            exactVars.Add(variable);
                        else if (tier == CandidateTier.SameSubject && policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.SameSubject))
                            sameSubjectVars.Add(variable);
                        else if (tier == CandidateTier.FacultyMember)
                            facultyMemberVars.Add(variable);
                        else
                            emergencyVars.Add(variable);

                        if (IsSubjectSpecialist(tier))
                        {
                            var formatPriority = GetExamFormatPriority(schedule.ExamFormatDisplay);
                            if (formatPriority == ExamFormatPriority.Oral && policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.OralSpecialist)) oralSpecialistVars.Add(variable);
                            if (formatPriority == ExamFormatPriority.Practical && policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.PracticalSpecialist)) practicalSpecialistVars.Add(variable);
                        }

                        if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Location))
                        {
                            var fixedLocationCost = CalculateSameDayLocationCost(lecturer.PersonKey, day, schedule, sameDayLocationMap);
                            if (fixedLocationCost is int cost && cost > 0)
                                locationCostTerms.Add(LinearExpr.Term(variable, cost * policy.GetWeight(AutoAssignmentPolicyRuleCodes.Location, 45)));
                        }
                    }

                    var shortage = model.NewIntVar(0, need, $"shortage_s{schedule.ExamScheduleId}");
                    shortageVars.Add(shortage);
                    var coverageTerms = scheduleVars.Select(x => (LinearExpr)x).Append(shortage).ToArray();
                    model.Add(LinearExpr.Sum(coverageTerms) == need);
                }

                foreach (var group in variables.GroupBy(x =>
                {
                    var schedule = scheduleById[x.Key.ScheduleId];
                    return (x.Key.LecturerId, schedule.SlotId, Day: DateOnly.FromDateTime(schedule.ExamDate));
                }))
                {
                    model.Add(LinearExpr.Sum(group.Select(x => (LinearExpr)x.Value).ToArray()) <= policy.MaxAssignmentsPerSlot);
                }

                var expectedNewAssignments = processableSchedules.Sum(x =>
                {
                    var assignedCount = scheduleAssignedUsers.TryGetValue(x.ExamScheduleId, out var assigned) ? assigned.Count : 0;
                    return Math.Max(0, GetRequiredInvigilators(x, policy) - assignedCount);
                });
                var targetLoad = lecturers.Count == 0
                    ? 0
                    : (int)Math.Ceiling((lecturerLoads.Values.Sum() + expectedNewAssignments) / (double)lecturers.Count);

                foreach (var lecturer in lecturers)
                {
                    var lecturerVars = variables
                        .Where(x => x.Key.LecturerId == lecturer.UserId)
                        .Select(x => (LinearExpr)x.Value)
                        .ToArray();
                    var currentLoad = lecturerLoads.TryGetValue(lecturer.UserId, out var load) ? load : 0;
                    if (policy.MaxAssignmentsPerPeriod.HasValue)
                    {
                        var allowedAdditional = Math.Max(0, policy.MaxAssignmentsPerPeriod.Value - currentLoad);
                        model.Add(LinearExpr.Sum(lecturerVars.Append(LinearExpr.Constant(0)).ToArray()) <= allowedAdditional);
                    }

                    var maxLoad = currentLoad + lecturerVars.Length;
                    var loadVar = model.NewIntVar(currentLoad, maxLoad, $"load_u{lecturer.UserId}");
                    model.Add(loadVar == LinearExpr.Sum(lecturerVars.Append(LinearExpr.Constant(currentLoad)).ToArray()));

                    var deviation = model.NewIntVar(0, Math.Max(maxLoad, targetLoad) + currentLoad + 1, $"dev_u{lecturer.UserId}");
                    model.AddAbsEquality(deviation, loadVar - targetLoad);
                    if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.LowLoad))
                        fairnessTerms.Add(LinearExpr.Term(deviation, policy.GetWeight(AutoAssignmentPolicyRuleCodes.LowLoad, 700)));
                }

                foreach (var group in variables.GroupBy(x =>
                {
                    var schedule = scheduleById[x.Key.ScheduleId];
                    return (x.Key.LecturerId, Day: DateOnly.FromDateTime(schedule.ExamDate));
                }))
                {
                    var existingDayLoad = sameDayLoadMap.TryGetValue(group.Key, out var dayLoad) ? dayLoad : 0;
                    if (policy.MaxAssignmentsPerDay.HasValue)
                    {
                        var allowedAdditional = Math.Max(0, policy.MaxAssignmentsPerDay.Value - existingDayLoad);
                        model.Add(LinearExpr.Sum(group.Select(x => (LinearExpr)x.Value).Append(LinearExpr.Constant(0)).ToArray()) <= allowedAdditional);
                    }

                    var dayVar = model.NewIntVar(existingDayLoad, existingDayLoad + group.Count(), $"day_u{group.Key.LecturerId}_{group.Key.Day:yyyyMMdd}");
                    model.Add(dayVar == LinearExpr.Sum(group.Select(x => (LinearExpr)x.Value).Append(LinearExpr.Constant(existingDayLoad)).ToArray()));

                    var overload = model.NewIntVar(0, existingDayLoad + group.Count(), $"day_over_u{group.Key.LecturerId}_{group.Key.Day:yyyyMMdd}");
                    model.Add(overload >= dayVar - 1);
                    if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.SameDayLoad))
                        fairnessTerms.Add(LinearExpr.Term(overload, policy.GetWeight(AutoAssignmentPolicyRuleCodes.SameDayLoad, 600)));
                }

                foreach (var group in variables.GroupBy(x =>
                {
                    var schedule = scheduleById[x.Key.ScheduleId];
                    return (x.Key.LecturerId, Day: DateOnly.FromDateTime(schedule.ExamDate), schedule.SessionId);
                }))
                {
                    var groupItems = group.ToList();
                    for (var i = 0; i < groupItems.Count; i++)
                    {
                        for (var j = i + 1; j < groupItems.Count; j++)
                        {
                            var firstSchedule = scheduleById[groupItems[i].Key.ScheduleId];
                            var secondSchedule = scheduleById[groupItems[j].Key.ScheduleId];
                            if (!policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Location))
                                continue;

                            var distanceCost = CalculateRoomDistanceCost(firstSchedule, secondSchedule);
                            if (distanceCost == 0)
                                continue;

                            var pair = model.NewBoolVar($"loc_u{group.Key.LecturerId}_s{firstSchedule.ExamScheduleId}_s{secondSchedule.ExamScheduleId}");
                            model.AddMultiplicationEquality(pair, new IntVar[] { groupItems[i].Value, groupItems[j].Value });
                            locationCostTerms.Add(LinearExpr.Term(pair, distanceCost * policy.GetWeight(AutoAssignmentPolicyRuleCodes.Location, 45)));
                        }
                    }
                }

                var solverSeed = Math.Max(1, request.RunSeed.GetValueOrDefault(1));
                var stopwatch = Stopwatch.StartNew();
                var solver = new CpSolver();
                List<(int ScheduleId, int LecturerId)>? bestSelectedAssignments = null;
                var isOptimizationProven = true;
                var hasSolutionHint = false;

                CpSolverStatus SolveWithRemainingTime()
                {
                    var remainingSeconds = InternalSolverTimeLimitSeconds - stopwatch.Elapsed.TotalSeconds;
                    if (remainingSeconds < MinimumSolverPhaseSeconds)
                        return CpSolverStatus.Unknown;

                    solver.StringParameters =
                        $"max_time_in_seconds:{remainingSeconds.ToString("0.###", CultureInfo.InvariantCulture)} num_search_workers:1 random_seed:{solverSeed} randomize_search:true";
                    return solver.Solve(model);
                }

                static bool HasSolution(CpSolverStatus status)
                    => status is CpSolverStatus.Feasible or CpSolverStatus.Optimal;

                List<(int ScheduleId, int LecturerId)> CaptureSelectedAssignments()
                {
                    return variables
                        .Where(x => solver.Value(x.Value) == 1)
                        .Select(x => x.Key)
                        .ToList();
                }

                void RememberCurrentSolution(CpSolverStatus status)
                {
                    bestSelectedAssignments = CaptureSelectedAssignments();
                    isOptimizationProven &= status == CpSolverStatus.Optimal;
                }

                void AddCurrentSolutionHint()
                {
                    if (hasSolutionHint || bestSelectedAssignments == null || bestSelectedAssignments.Count == 0)
                        return;

                    foreach (var key in bestSelectedAssignments)
                    {
                        if (variables.TryGetValue(key, out var variable))
                            model.AddHint(variable, 1);
                    }

                    hasSolutionHint = true;
                }

                CpSatAssignmentResult? BuildBestResult()
                    => bestSelectedAssignments == null
                        ? null
                        : BuildCpSatResultFromSelection(bestSelectedAssignments, isOptimizationProven);

                CpSatAssignmentResult BuildCpSatResultFromSelection(
                    List<(int ScheduleId, int LecturerId)> selectedKeys,
                    bool optimizationProven)
                {
                    var resultPlan = new AutoAssignPlanDto();
                    var resultDetails = schedules.ToDictionary(
                        x => x.ExamScheduleId,
                        x => new AutoAssignScheduleResultDto
                        {
                            ExamScheduleId = x.ExamScheduleId,
                            ExamDate = x.ExamDate,
                            SlotName = x.SlotName,
                            RoomDisplay = x.RoomDisplay,
                            SubjectName = x.SubjectName,
                            ClassName = x.ClassName,
                            ExamFormatDisplay = x.ExamFormatDisplay,
                            StatusBefore = x.Status,
                            RequiredCount = GetRequiredInvigilators(x, policy),
                            AssignedCount = scheduleAssignedUsers.TryGetValue(x.ExamScheduleId, out var assigned) ? assigned.Count : 0
                        });

                    foreach (var schedule in schedules.Where(x => !processableScheduleIds.Contains(x.ExamScheduleId)))
                    {
                        if (IsFinalStatus(schedule.Status))
                            resultDetails[schedule.ExamScheduleId] = CreateSkippedDetail(schedule, GetRequiredInvigilators(schedule, policy));
                        else if (IsSkippedByFormat(schedule, policy))
                        {
                            resultDetails[schedule.ExamScheduleId].StatusAfter = schedule.Status;
                            resultDetails[schedule.ExamScheduleId].Message = "Bỏ qua phân công theo cấu hình hình thức thi.";
                        }
                    }

                    var mutableAssignedUsers = scheduleAssignedUsers.ToDictionary(x => x.Key, x => x.Value.ToHashSet());
                    var mutableAssignedPositions = scheduleAssignedPositions.ToDictionary(x => x.Key, x => x.Value.ToHashSet());
                    var mutableLecturerLoads = lecturerLoads.ToDictionary(x => x.Key, x => x.Value);
                    var mutableSameDayLoadMap = sameDayLoadMap.ToDictionary(x => x.Key, x => x.Value);
                    var mutableSameDayLocationMap = sameDayLocationMap.ToDictionary(x => x.Key, x => x.Value.ToList());
                    var mutableOccupiedKeySet = occupiedKeySet.ToHashSet();
                    var selectedKeySet = selectedKeys.ToHashSet();

                    var selectedAssignments = selectedKeySet
                        .Where(x => scheduleById.ContainsKey(x.ScheduleId) && lecturerById.ContainsKey(x.LecturerId))
                        .Select(x => new
                        {
                            Schedule = scheduleById[x.ScheduleId],
                            Lecturer = lecturerById[x.LecturerId]
                        })
                        .OrderBy(x => GetCandidateTier(x.Lecturer, x.Schedule, subjectLecturerMap, isLecturerRoleByUser))
                        .ThenBy(x => x.Schedule.ExamDate)
                        .ThenBy(x => x.Schedule.TimeStart)
                        .ThenBy(x => x.Lecturer.UserName)
                        .ToList();

                    foreach (var selected in selectedAssignments)
                    {
                        if (!mutableAssignedUsers.TryGetValue(selected.Schedule.ExamScheduleId, out var assignedUsers))
                        {
                            assignedUsers = new HashSet<int>();
                            mutableAssignedUsers[selected.Schedule.ExamScheduleId] = assignedUsers;
                        }

                        if (!mutableAssignedPositions.TryGetValue(selected.Schedule.ExamScheduleId, out var assignedPositions))
                        {
                            assignedPositions = new HashSet<byte>();
                            mutableAssignedPositions[selected.Schedule.ExamScheduleId] = assignedPositions;
                        }

                        var day = DateOnly.FromDateTime(selected.Schedule.ExamDate);
                        var load = mutableLecturerLoads.TryGetValue(selected.Lecturer.UserId, out var currentLoad) ? currentLoad : 0;
                        var sameDayLoad = mutableSameDayLoadMap.TryGetValue((selected.Lecturer.PersonKey, day), out var d) ? d : 0;
                        var tier = GetCandidateTier(selected.Lecturer, selected.Schedule, subjectLecturerMap, isLecturerRoleByUser);
                        var locationCost = CalculateSameDayLocationCost(selected.Lecturer.PersonKey, day, selected.Schedule, mutableSameDayLocationMap);
                        var score = GetCandidateScore(tier, load, sameDayLoad, locationCost, policy);
                        var reasonParts = new[]
                            {
                                GetFormatSpecialistReason(tier, selected.Schedule, policy),
                                GetCandidateTierReason(tier, policy),
                                policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Location) ? GetLocationReason(locationCost) : string.Empty
                            }
                            .Where(x => !string.IsNullOrWhiteSpace(x));
                        var reason = string.Join("; ", reasonParts);
                        if (string.IsNullOrWhiteSpace(reason))
                            reason = "Đáp ứng ràng buộc bắt buộc";

                        AssignOne(
                            resultPlan,
                            resultDetails[selected.Schedule.ExamScheduleId],
                            selected.Schedule,
                            selected.Lecturer,
                            request.AssignerId,
                            assignedUsers,
                            assignedPositions,
                            mutableLecturerLoads,
                            mutableSameDayLoadMap,
                            mutableSameDayLocationMap,
                            mutableOccupiedKeySet,
                            GetRequiredInvigilators(selected.Schedule, policy),
                            score,
                            reason);
                    }

                    foreach (var schedule in processableSchedules)
                    {
                        var assignedCount = mutableAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers)
                            ? assignedUsers.Count
                            : 0;
                        var requiredCount = GetRequiredInvigilators(schedule, policy);
                        var statusAfter = requiredCount == 0
                            ? schedule.Status
                            : assignedCount >= requiredCount ? "Chờ duyệt" : "Thiếu giám thị";
                        resultPlan.ScheduleStatuses.Add(new AutoAssignScheduleStatusUpdateDto
                        {
                            ExamScheduleId = schedule.ExamScheduleId,
                            Status = statusAfter
                        });

                        var detail = resultDetails[schedule.ExamScheduleId];
                        detail.AssignedCount = assignedCount;
                        detail.StatusAfter = statusAfter;
                        detail.Message = requiredCount == 0
                            ? "Bỏ qua phân công theo cấu hình hình thức thi."
                            : assignedCount >= requiredCount
                            ? (assignmentMode == AutoAssignmentMode.RepairRejected
                                ? "Đã bổ sung giám thị thay thế và đưa lịch về trạng thái chờ duyệt lại."
                                : $"Đã phân công đủ {requiredCount} giám thị theo các tiêu chí ưu tiên.")
                            : $"Chưa tìm đủ giảng viên phù hợp, còn thiếu {requiredCount - assignedCount} giám thị.";
                    }

                    foreach (var schedule in schedules.Where(x => !processableScheduleIds.Contains(x.ExamScheduleId)))
                    {
                        var assignedCount = mutableAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers)
                            ? assignedUsers.Count
                            : 0;
                        var detail = resultDetails[schedule.ExamScheduleId];
                        if (string.IsNullOrWhiteSpace(detail.StatusAfter))
                            detail.StatusAfter = schedule.Status;
                        detail.AssignedCount = assignedCount;
                        if (string.IsNullOrWhiteSpace(detail.Message))
                            detail.Message = "Không gán mới.";
                    }

                    var cpSatResult = new AutoAssignResultDto
                    {
                        Success = true,
                        IsOptimizationProven = optimizationProven,
                        AssignerId = request.AssignerId,
                        TotalSchedules = schedules.Count,
                        AssignedInvigilators = resultPlan.NewInvigilators.Count,
                        FullyAssignedSchedules = resultDetails.Values.Count(x => x.StatusAfter == "Chờ duyệt"),
                        MissingSchedules = resultDetails.Values.Count(x => x.StatusAfter == "Thiếu giám thị"),
                        Details = schedules.Select(x => resultDetails[x.ExamScheduleId]).ToList(),
                        Message = assignmentMode == AutoAssignmentMode.RepairRejected
                            ? "Đã hoàn tất bổ sung giám thị cho các lịch cần xử lý lại."
                            : "Đã hoàn tất tự động phân công giám thị."
                    };

                    if (cpSatResult.MissingSchedules > 0)
                        cpSatResult.Warnings.Add("Một số lịch vẫn chưa đủ giám thị do không còn giảng viên phù hợp theo lịch bận và trạng thái hiện tại.");

                    return new CpSatAssignmentResult(resultPlan, cpSatResult);
                }

                var maxShortage = processableSchedules.Sum(x => GetRequiredInvigilators(x, policy));
                var totalShortage = AddSumVar(model, shortageVars.Select(x => (LinearExpr)x), "total_shortage", 0, maxShortage);
                var oralSpecialistTotal = AddSumVar(model, oralSpecialistVars.Select(x => (LinearExpr)x), "total_oral_specialist", 0, oralSpecialistVars.Count);
                var practicalSpecialistTotal = AddSumVar(model, practicalSpecialistVars.Select(x => (LinearExpr)x), "total_practical_specialist", 0, practicalSpecialistVars.Count);
                var exactTotal = AddSumVar(model, exactVars.Select(x => (LinearExpr)x), "total_exact", 0, exactVars.Count);
                var sameSubjectTotal = AddSumVar(model, sameSubjectVars.Select(x => (LinearExpr)x), "total_same_subject", 0, sameSubjectVars.Count);

                model.Minimize(totalShortage);
                var status = SolveWithRemainingTime();
                if (!HasSolution(status))
                    return null;

                RememberCurrentSolution(status);
                AddCurrentSolutionHint();

                var bestShortage = (int)solver.Value(totalShortage);
                model.Add(totalShortage == bestShortage);

                if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.OralSpecialist) && oralSpecialistVars.Count > 0)
                {
                    model.Maximize(oralSpecialistTotal);
                    status = SolveWithRemainingTime();
                    if (!HasSolution(status))
                        return BuildBestResult();

                    RememberCurrentSolution(status);
                    AddCurrentSolutionHint();

                    var bestOralSpecialist = (int)solver.Value(oralSpecialistTotal);
                    model.Add(oralSpecialistTotal == bestOralSpecialist);
                }

                if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.PracticalSpecialist) && practicalSpecialistVars.Count > 0)
                {
                    model.Maximize(practicalSpecialistTotal);
                    status = SolveWithRemainingTime();
                    if (!HasSolution(status))
                        return BuildBestResult();

                    RememberCurrentSolution(status);
                    AddCurrentSolutionHint();

                    var bestPracticalSpecialist = (int)solver.Value(practicalSpecialistTotal);
                    model.Add(practicalSpecialistTotal == bestPracticalSpecialist);
                }

                if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.ExactOwner) && exactVars.Count > 0)
                {
                    model.Maximize(exactTotal);
                    status = SolveWithRemainingTime();
                    if (!HasSolution(status))
                        return BuildBestResult();

                    RememberCurrentSolution(status);
                    AddCurrentSolutionHint();

                    var bestExact = (int)solver.Value(exactTotal);
                    model.Add(exactTotal == bestExact);
                }

                if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.SameSubject) && sameSubjectVars.Count > 0)
                {
                    model.Maximize(sameSubjectTotal);
                    status = SolveWithRemainingTime();
                    if (!HasSolution(status))
                        return BuildBestResult();

                    RememberCurrentSolution(status);
                    AddCurrentSolutionHint();

                    var bestSameSubject = (int)solver.Value(sameSubjectTotal);
                    model.Add(sameSubjectTotal == bestSameSubject);
                }

                if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Emergency))
                    fairnessTerms.AddRange(emergencyVars.Select(x => LinearExpr.Term(x, policy.GetWeight(AutoAssignmentPolicyRuleCodes.Emergency, 3_000))));
                if (policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.FacultyMember))
                    fairnessTerms.AddRange(facultyMemberVars.Select(x => LinearExpr.Term(x, policy.GetWeight(AutoAssignmentPolicyRuleCodes.FacultyMember, 8_000))));
                fairnessTerms.AddRange(locationCostTerms);
                if (fairnessTerms.Count > 0)
                {
                    model.Minimize(LinearExpr.Sum(fairnessTerms.ToArray()));
                    status = SolveWithRemainingTime();
                    if (!HasSolution(status))
                        return BuildBestResult();

                    RememberCurrentSolution(status);
                }

                return BuildBestResult();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsFeasibleCpSatCandidate(
            AutoAssignLecturerDto lecturer,
            AutoAssignScheduleDto schedule,
            HashSet<int> assignedUsers,
            Dictionary<int, int> lecturerLoads,
            Dictionary<(int PersonKey, DateOnly Day), int> sameDayLoadMap,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            AutoAssignmentPolicyDto policy)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var busyKey = (lecturer.UserId, schedule.SlotId, day);
            var occupiedKey = (lecturer.PersonKey, schedule.SlotId, day);

            if (!lecturer.IsActive ||
                assignedUsers.Contains(lecturer.PersonKey) ||
                busyKeySet.Contains(busyKey) ||
                occupiedKeySet.Contains(occupiedKey))
                return false;

            if (policy.MaxAssignmentsPerPeriod.HasValue &&
                lecturerLoads.TryGetValue(lecturer.UserId, out var currentLoad) &&
                currentLoad >= policy.MaxAssignmentsPerPeriod.Value)
                return false;

            if (policy.MaxAssignmentsPerDay.HasValue &&
                sameDayLoadMap.TryGetValue((lecturer.PersonKey, day), out var sameDayLoad) &&
                sameDayLoad >= policy.MaxAssignmentsPerDay.Value)
                return false;

            return true;
        }

        private static IntVar AddSumVar(
            CpModel model,
            IEnumerable<LinearExpr> terms,
            string name,
            int lowerBound,
            int upperBound)
        {
            var variable = model.NewIntVar(lowerBound, Math.Max(lowerBound, upperBound), name);
            model.Add(variable == LinearExpr.Sum(terms.Append(LinearExpr.Constant(0)).ToArray()));
            return variable;
        }

        private static bool CanProcessSchedule(
            AutoAssignScheduleDto schedule,
            Dictionary<int, HashSet<int>> scheduleAssignedUsers,
            int requiredInvigilators)
        {
            if (schedule.Status.Equals("Từ chối duyệt", StringComparison.OrdinalIgnoreCase))
                return false;

            var assignedCount = scheduleAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers)
                ? assignedUsers.Count
                : 0;
            return assignedCount < requiredInvigilators;
        }

        private static int GetRequiredInvigilators(AutoAssignScheduleDto schedule, AutoAssignmentPolicyDto policy)
        {
            return policy.GetAssignmentMode(schedule.ExamFormatId) switch
            {
                AutoAssignmentExamFormatAssignmentModes.Skip => 0,
                AutoAssignmentExamFormatAssignmentModes.OwnerOnly => 1,
                _ => policy.RequiredInvigilatorsPerSchedule
            };
        }

        private static bool IsOwnerOnly(AutoAssignScheduleDto schedule, AutoAssignmentPolicyDto policy)
        {
            return string.Equals(
                policy.GetAssignmentMode(schedule.ExamFormatId),
                AutoAssignmentExamFormatAssignmentModes.OwnerOnly,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSkippedByFormat(AutoAssignScheduleDto schedule, AutoAssignmentPolicyDto policy)
        {
            return string.Equals(
                policy.GetAssignmentMode(schedule.ExamFormatId),
                AutoAssignmentExamFormatAssignmentModes.Skip,
                StringComparison.OrdinalIgnoreCase);
        }

        private static CandidateTier GetCandidateTier(
            AutoAssignLecturerDto lecturer,
            AutoAssignScheduleDto schedule,
            Dictionary<string, HashSet<int>> subjectLecturerMap,
            Dictionary<int, bool> isLecturerRoleByUser)
        {
            if (lecturer.PersonKey == schedule.OfferingUserPersonKey)
                return CandidateTier.ExactOwner;

            if (isLecturerRoleByUser.TryGetValue(lecturer.UserId, out var isLecturerRole) && !isLecturerRole)
                return CandidateTier.FacultyMember;

            return subjectLecturerMap.TryGetValue(schedule.SubjectId, out var lecturerIds) && lecturerIds.Contains(lecturer.UserId)
                ? CandidateTier.SameSubject
                : CandidateTier.Emergency;
        }

        private static bool IsSubjectSpecialist(CandidateTier tier)
            => tier is CandidateTier.ExactOwner or CandidateTier.SameSubject;

        private static ExamFormatPriority GetExamFormatPriority(string? examFormatDisplay)
        {
            var code = NormalizeExamFormatCode(examFormatDisplay);
            if (code is "VD" or "BTL-VD" or "TL-VD" or "NTL-VD") return ExamFormatPriority.Oral;
            if (code is "PM" or "DA" or "TH") return ExamFormatPriority.Practical;
            return ExamFormatPriority.Standard;
        }

        private static string NormalizeExamFormatCode(string? value)
        {
            var raw = (value ?? string.Empty).Trim();
            var separator = raw.IndexOf(" - ", StringComparison.Ordinal);
            var code = (separator >= 0 ? raw[..separator] : raw).Trim().ToUpperInvariant();
            code = System.Text.RegularExpressions.Regex.Replace(code, @"\s*[-/]\s*", "-");
            return code switch
            {
                "TN-TL" => "TN-TL",
                "BTL-VD" => "BTL-VD",
                "TL-VD" => "TL-VD",
                "NTL-VD" => "NTL-VD",
                "PTH" => "TH",
                _ => code
            };
        }

        private static int GetCandidateBaseCost(CandidateTier tier)
        {
            return tier switch
            {
                CandidateTier.ExactOwner => -50_000,
                CandidateTier.SameSubject => 1_000,
                CandidateTier.Emergency => 12_000,
                _ => 28_000
            };
        }

        private static int GetCandidateScore(
            CandidateTier tier,
            int load,
            int sameDayLoad,
            int? locationCost,
            AutoAssignmentPolicyDto policy)
        {
            var baseScore = tier switch
            {
                CandidateTier.ExactOwner => policy.GetWeight(AutoAssignmentPolicyRuleCodes.ExactOwner, 12_000),
                CandidateTier.SameSubject => policy.GetWeight(AutoAssignmentPolicyRuleCodes.SameSubject, 8_000),
                CandidateTier.Emergency => policy.GetWeight(AutoAssignmentPolicyRuleCodes.Emergency, 3_000),
                _ => policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.FacultyMember)
                    ? Math.Max(0, 2_000 - policy.GetWeight(AutoAssignmentPolicyRuleCodes.FacultyMember, 8_000) / 4)
                    : 0
            };

            var loadPenalty = policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.LowLoad) ? load * 120 : 0;
            var dayPenalty = policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.SameDayLoad) ? sameDayLoad * 120 : 0;
            var locationBonus = policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Location) ? GetLocationScoreBonus(locationCost) : 0;
            return Math.Max(0, baseScore - loadPenalty - dayPenalty + locationBonus);
        }

        private static int? CalculateSameDayLocationCost(
            int personKey,
            DateOnly day,
            AutoAssignScheduleDto targetSchedule,
            Dictionary<(int PersonKey, DateOnly Day, int SessionId), List<AutoAssignScheduleDto>> sameDayLocationMap)
        {
            if (!sameDayLocationMap.TryGetValue((personKey, day, targetSchedule.SessionId), out var sameDaySchedules) || sameDaySchedules.Count == 0)
                return null;

            return sameDaySchedules.Min(x => CalculateRoomDistanceCost(x, targetSchedule));
        }

        private static int CalculateRoomDistanceCost(AutoAssignScheduleDto first, AutoAssignScheduleDto second)
        {
            var firstLocation = ParseRoomLocation(first.RoomDisplay);
            var secondLocation = ParseRoomLocation(second.RoomDisplay);

            if (firstLocation.NormalizedRoom == secondLocation.NormalizedRoom)
                return 0;

            if (firstLocation.BuildingKey == secondLocation.BuildingKey)
            {
                if (firstLocation.Floor.HasValue && secondLocation.Floor.HasValue)
                {
                    if (firstLocation.Floor.Value == secondLocation.Floor.Value)
                        return 10;

                    return 20 + Math.Min(30, Math.Abs(firstLocation.Floor.Value - secondLocation.Floor.Value) * 5);
                }

                return 35;
            }

            return firstLocation.BuildingGroupKey == secondLocation.BuildingGroupKey ? 55 : 120;
        }

        private static int GetLocationScoreBonus(int? locationCost)
        {
            if (!locationCost.HasValue)
                return 0;

            return locationCost.Value switch
            {
                0 => 450,
                <= 10 => 350,
                <= 35 => 180,
                <= 55 => 60,
                _ => -180
            };
        }

        private static int StableJitter(int runSeed, int scheduleId, int lecturerId)
        {
            unchecked
            {
                var hash = runSeed;
                hash = (hash * 397) ^ scheduleId;
                hash = (hash * 397) ^ lecturerId;
                return hash & 0x7fffffff;
            }
        }

        private static string GetLocationReason(int? locationCost)
        {
            if (!locationCost.HasValue)
                return string.Empty;

            return locationCost.Value switch
            {
                0 => "Ưu tiên cùng phòng trong ngày",
                <= 10 => "Ưu tiên cùng giảng đường, cùng tầng",
                <= 35 => "Ưu tiên cùng giảng đường",
                <= 55 => "Ưu tiên khu giảng đường gần",
                _ => "Hạn chế di chuyển xa giữa các ca"
            };
        }

        private static RoomLocation ParseRoomLocation(string? roomDisplay)
        {
            var normalized = NormalizeRoomDisplay(roomDisplay);
            if (normalized.StartsWith("TH.HAI LY", StringComparison.Ordinal) || normalized.StartsWith("TH HAI LY", StringComparison.Ordinal))
                return new RoomLocation(normalized, "TH.HAI LY", "C_TH_HAI_LY", null);

            var parts = normalized
                .Replace('-', '.')
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var building = parts.Length > 0 ? parts[0] : normalized;
            if (string.IsNullOrWhiteSpace(building))
                building = normalized;

            var buildingGroup = building.StartsWith("C", StringComparison.Ordinal) && building.Length > 1 && char.IsDigit(building[1])
                ? "C_TH_HAI_LY"
                : building;
            var floor = ExtractFloor(parts);

            return new RoomLocation(normalized, building, buildingGroup, floor);
        }

        private static int? ExtractFloor(string[] roomParts)
        {
            if (roomParts.Length < 2)
                return null;

            var floorPart = roomParts[1];
            if (floorPart.Length == 0 || !char.IsDigit(floorPart[0]))
                return null;

            if (floorPart.Length >= 2 && char.IsDigit(floorPart[1]) && roomParts.Length > 2)
            {
                return int.TryParse(floorPart, out var multiDigitFloor) ? multiDigitFloor : null;
            }

            return int.TryParse(floorPart[0].ToString(), out var floor) ? floor : null;
        }

        private static string NormalizeRoomDisplay(string? roomDisplay)
        {
            var normalized = (roomDisplay ?? string.Empty).Trim().ToUpperInvariant();
            normalized = normalized.Replace('_', ' ').Replace('/', '.');
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*[.-]\s*", ".");
            return normalized;
        }

        private static string GetCandidateTierReason(CandidateTier tier, AutoAssignmentPolicyDto policy)
        {
            return tier switch
            {
                CandidateTier.ExactOwner when policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.ExactOwner) => "Đang dạy lớp học phần",
                CandidateTier.SameSubject when policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.SameSubject) => "Có chuyên môn môn thi",
                CandidateTier.Emergency when policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.Emergency) => "Phù hợp lịch",
                CandidateTier.FacultyMember when policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.FacultyMember) => "Dự phòng khi thiếu giảng viên",
                _ => string.Empty
            };
        }

        private static string GetFormatSpecialistReason(CandidateTier tier, AutoAssignScheduleDto schedule, AutoAssignmentPolicyDto policy)
        {
            if (!IsSubjectSpecialist(tier))
                return string.Empty;

            var formatPriority = GetExamFormatPriority(schedule.ExamFormatDisplay);
            return formatPriority switch
            {
                ExamFormatPriority.Oral when policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.OralSpecialist) => "Ưu tiên chuyên môn cho thi vấn đáp",
                ExamFormatPriority.Practical when policy.IsRuleEnabled(AutoAssignmentPolicyRuleCodes.PracticalSpecialist) => "Ưu tiên chuyên môn cho thi thực hành",
                _ => string.Empty
            };
        }

        private static bool IsFinalStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals("Đã duyệt", StringComparison.OrdinalIgnoreCase)
                   || status.Equals("Từ chối duyệt", StringComparison.OrdinalIgnoreCase);
        }

        private sealed record FallbackCandidate(
            AutoAssignLecturerDto Lecturer,
            int Score,
            int TieBreaker,
            string Reason);

        private sealed record CpSatAssignmentResult(
            AutoAssignPlanDto Plan,
            AutoAssignResultDto Result);

        private sealed record CachedPreviewPlan(
            int AssignerId,
            int SemesterId,
            int PeriodId,
            AutoAssignPlanDto Plan,
            AutoAssignResultDto Result,
            DateTime CreatedAtUtc);

        private sealed record RoomLocation(
            string NormalizedRoom,
            string BuildingKey,
            string BuildingGroupKey,
            int? Floor);

        private enum CandidateTier
        {
            ExactOwner = 0,
            SameSubject = 1,
            Emergency = 2,
            FacultyMember = 3
        }

        private enum AutoAssignmentMode
        {
            InitialAssignment = 0,
            RepairRejected = 1
        }

        private enum ExamFormatPriority
        {
            Standard = 0,
            Practical = 1,
            Oral = 2
        }
    }
}
