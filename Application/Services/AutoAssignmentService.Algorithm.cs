using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Common.Constants;

namespace ExamInvigilationManagement.Application.Services
{
    public partial class AutoAssignmentService
    {
        private async Task<AutoAssignResultDto> BuildAssignmentAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var facultyId = await _repository.GetUserFacultyIdAsync(request.AssignerId, cancellationToken);
            if (facultyId is null || facultyId <= 0)
                throw new ArgumentException("Không xác định được khoa của người thực hiện phân công.");

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

            var approvalRejectedScheduleIds = schedules
                .Where(x => IsApprovalRejectedStatus(x.Status))
                .Select(x => x.ExamScheduleId)
                .ToHashSet();

            var blockedPreviousAssigneesBySchedule = existingAssignments
                .Where(x => approvalRejectedScheduleIds.Contains(x.ExamScheduleId) && !x.IsCancelled)
                .GroupBy(x => x.ExamScheduleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.PersonKey).ToHashSet());

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
                .Where(x => !x.IsInactive && !approvalRejectedScheduleIds.Contains(x.ExamScheduleId))
                .ToList();
            var assignmentMode = existingAssignments.Any(x => x.IsRejected) || approvalRejectedScheduleIds.Count > 0
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
                blockedPreviousAssigneesBySchedule,
                existingAssignments
                    .Where(x => approvalRejectedScheduleIds.Contains(x.ExamScheduleId) && !x.IsCancelled)
                    .Select(x => x.ExamInvigilatorId)
                    .Distinct()
                    .ToList(),
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
                        occupiedKeySet,
                        blockedPreviousAssigneesBySchedule);

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
            plan.CancelledExistingInvigilatorIds.AddRange(existingAssignments
                .Where(x => approvalRejectedScheduleIds.Contains(x.ExamScheduleId) && !x.IsCancelled)
                .Select(x => x.ExamInvigilatorId)
                .Distinct());
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
                    IsFeasibleExactOwner(x, schedule, assignedUsers, busyKeySet, occupiedKeySet, blockedPreviousAssigneesBySchedule) &&
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
                        blockedPreviousAssigneesBySchedule,
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
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            Dictionary<int, HashSet<int>> blockedPreviousAssigneesBySchedule)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);

            if (!scheduleAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers))
                assignedUsers = new HashSet<int>();

            var hasExactOwner = lecturers.Any(l =>
                l.PersonKey == schedule.OfferingUserPersonKey &&
                IsFeasibleExactOwner(l, schedule, assignedUsers, busyKeySet, occupiedKeySet, blockedPreviousAssigneesBySchedule));

            blockedPreviousAssigneesBySchedule.TryGetValue(schedule.ExamScheduleId, out var blockedPreviousAssignees);

            var fallbackCandidates = lecturers.Count(l =>
                l.IsActive &&
                l.PersonKey != schedule.OfferingUserPersonKey &&
                !assignedUsers.Contains(l.PersonKey) &&
                !(blockedPreviousAssignees?.Contains(l.PersonKey) ?? false) &&
                !busyKeySet.Contains((l.UserId, schedule.SlotId, day)) &&
                !occupiedKeySet.Contains((l.PersonKey, schedule.SlotId, day)));

            return (fallbackCandidates + (hasExactOwner ? 1 : 0), hasExactOwner);
        }

        private static bool IsFeasibleExactOwner(
            AutoAssignLecturerDto lecturer,
            AutoAssignScheduleDto schedule,
            HashSet<int> assignedUsers,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            Dictionary<int, HashSet<int>> blockedPreviousAssigneesBySchedule)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var busyKey = (lecturer.UserId, schedule.SlotId, day);
            var occupiedKey = (lecturer.PersonKey, schedule.SlotId, day);
            var isPreviouslyRejectedAssignee = blockedPreviousAssigneesBySchedule.TryGetValue(schedule.ExamScheduleId, out var blockedAssignees) &&
                                               blockedAssignees.Contains(lecturer.PersonKey);

            return lecturer.IsActive
                   && lecturer.PersonKey == schedule.OfferingUserPersonKey
                   && !assignedUsers.Contains(lecturer.PersonKey)
                   && !isPreviouslyRejectedAssignee
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
            Dictionary<int, HashSet<int>> blockedPreviousAssigneesBySchedule,
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
                    if (!IsFeasibleFallback(l, schedule, assignedUsers, busyKeySet, occupiedKeySet, blockedPreviousAssigneesBySchedule) ||
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
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            Dictionary<int, HashSet<int>> blockedPreviousAssigneesBySchedule)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var busyKey = (lecturer.UserId, schedule.SlotId, day);
            var occupiedKey = (lecturer.PersonKey, schedule.SlotId, day);
            var isPreviouslyRejectedAssignee = blockedPreviousAssigneesBySchedule.TryGetValue(schedule.ExamScheduleId, out var blockedAssignees) &&
                                               blockedAssignees.Contains(lecturer.PersonKey);

            return lecturer.IsActive
                   && lecturer.PersonKey != schedule.OfferingUserPersonKey
                   && !assignedUsers.Contains(lecturer.PersonKey)
                   && !isPreviouslyRejectedAssignee
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

        private static bool CanProcessSchedule(
            AutoAssignScheduleDto schedule,
            Dictionary<int, HashSet<int>> scheduleAssignedUsers,
            int requiredInvigilators)
        {
            if (IsFinalStatus(schedule.Status))
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

            return status.Equals("Đã duyệt", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsApprovalRejectedStatus(string status)
        {
            return status.Equals("Từ chối duyệt", StringComparison.OrdinalIgnoreCase)
                   || status.Equals(ExamScheduleStatuses.ApprovalRejectedCode, StringComparison.OrdinalIgnoreCase);
        }

        private sealed record FallbackCandidate(
            AutoAssignLecturerDto Lecturer,
            int Score,
            int TieBreaker,
            string Reason);

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
