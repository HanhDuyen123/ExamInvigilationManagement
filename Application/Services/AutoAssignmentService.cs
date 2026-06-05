using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using Google.OrTools.Sat;

namespace ExamInvigilationManagement.Application.Services
{
    public class AutoAssignmentService : IAutoAssignmentService
    {
        private const int RequiredInvigilatorsPerSchedule = 2;

        private readonly IAutoAssignmentRepository _repository;

        public AutoAssignmentService(IAutoAssignmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<AutoAssignResultDto> AutoAssignAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            request.PreviewOnly = false;
            return await BuildAssignmentAsync(request, cancellationToken);
        }

        public async Task<AutoAssignResultDto> PreviewAsync(
            AutoAssignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            request.PreviewOnly = true;
            return await BuildAssignmentAsync(request, cancellationToken);
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

            if (whitelistedLecturerIds.Count > 0 || await _repository.HasPeriodAvailabilityListAsync(request.PeriodId.Value, facultyId.Value, cancellationToken))
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
                assignmentMode);

            if (solverResult != null)
            {
                solverResult.Result.IsPreview = request.PreviewOnly;
                solverResult.Result.SemesterId = request.SemesterId;
                solverResult.Result.PeriodId = request.PeriodId;
                if (!request.PreviewOnly)
                    await _repository.SavePlanAsync(solverResult.Plan, cancellationToken);
                else
                    solverResult.Result.Message = BuildPreviewMessage(solverResult.Result);
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
                    RequiredCount = RequiredInvigilatorsPerSchedule,
                    AssignedCount = assignedUsers.Count
                };
            }

            // PHASE 1: reserve exact owner cho từng lịch nếu khả dụng
            foreach (var item in orderedSchedules)
            {
                var schedule = item.Schedule;

                if (IsFinalStatus(schedule.Status))
                {
                    detailByScheduleId[schedule.ExamScheduleId] = CreateSkippedDetail(schedule);
                    continue;
                }

                var assignedUsers = scheduleAssignedUsers[schedule.ExamScheduleId];
                var assignedPositions = scheduleAssignedPositions[schedule.ExamScheduleId];
                var detail = detailByScheduleId[schedule.ExamScheduleId];
                var day = DateOnly.FromDateTime(schedule.ExamDate);

                var exactOwner = lecturers.FirstOrDefault(x =>
                    x.PersonKey == schedule.OfferingUserPersonKey &&
                    IsFeasibleExactOwner(x, schedule, assignedUsers, busyKeySet, occupiedKeySet));

                if (exactOwner != null)
                {
                    var score = CalculateExactOwnerScore(
                        exactOwner,
                        schedule,
                        lecturerLoads,
                        sameDayLoadMap,
                        sameDayLocationMap,
                        day);

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
                        score,
                        "đúng giảng viên phụ trách lớp");
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

                var need = Math.Max(0, RequiredInvigilatorsPerSchedule - assignedUsers.Count);

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
                        day);

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
                        fallback.Score,
                        fallback.Reason);

                    need--;
                }

                var finalCount = assignedUsers.Count;
                var finalStatus = finalCount >= RequiredInvigilatorsPerSchedule
                    ? "Chờ duyệt"
                    : "Thiếu giám thị";

                plan.ScheduleStatuses.Add(new AutoAssignScheduleStatusUpdateDto
                {
                    ExamScheduleId = schedule.ExamScheduleId,
                    Status = finalStatus
                });

                detail.AssignedCount = finalCount;
                detail.StatusAfter = finalStatus;
                detail.Message = finalCount >= RequiredInvigilatorsPerSchedule
                    ? "Đã phân công đủ 2 giám thị."
                    : $"Thiếu {RequiredInvigilatorsPerSchedule - finalCount} giám thị.";
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

            if (result.MissingSchedules > 0)
                result.Warnings.Add("Một số lịch không đủ 2 giám thị do không còn người phù hợp theo lịch bận, trùng ca hoặc dữ liệu chuyên môn hiện có.");

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

        private static AutoAssignScheduleResultDto CreateSkippedDetail(AutoAssignScheduleDto schedule)
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
                RequiredCount = RequiredInvigilatorsPerSchedule,
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
            DateOnly day)
        {
            var candidates = lecturers
                .Where(l => IsFeasibleFallback(l, schedule, assignedUsers, busyKeySet, occupiedKeySet))
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
                        score += specialtyPriority == ExamFormatPriority.Oral ? 9500 : specialtyPriority == ExamFormatPriority.Practical ? 7500 : 2500;
                        reasons.Add("Có chuyên môn môn thi");
                    }
                    else if (tier == CandidateTier.ExactOwner)
                    {
                        score += specialtyPriority == ExamFormatPriority.Oral ? 11000 : specialtyPriority == ExamFormatPriority.Practical ? 9000 : 5000;
                        reasons.Add("Đang dạy lớp học phần");
                    }
                    else if (tier == CandidateTier.FacultyMember)
                    {
                        score -= 6500;
                        reasons.Add($"Dự phòng từ role {l.RoleName}");
                    }
                    else
                    {
                        score -= 2500;
                        reasons.Add("Phù hợp lịch");
                    }

                    // Ưu tiên người ít tải
                    score += Math.Max(0, 1000 - load * 120);

                    // Ưu tiên ít ca trong ngày
                    score += Math.Max(0, 120 - sameDayLoad * 40);

                    score += GetLocationScoreBonus(locationCost);
                    var locationReason = GetLocationReason(locationCost);
                    if (!string.IsNullOrWhiteSpace(locationReason))
                        reasons.Add(locationReason);

                    // Phạt nhẹ nếu người này là owner của nhiều lịch khác trong batch
                    // để tránh làm họ bị “ăn mất” quá nhiều, nhưng không loại bỏ hoàn toàn
                    score -= ownerCount * 150;

                    return new FallbackCandidate(
                        Lecturer: l,
                        Score: score,
                        Reason: string.Join("; ", reasons));
                })
                .OrderByDescending(x => x.Score)
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
            DateOnly day)
        {
            var load = lecturerLoads.TryGetValue(lecturer.UserId, out var currentLoad) ? currentLoad : 0;
            var sameDayLoad = sameDayLoadMap.TryGetValue((lecturer.PersonKey, day), out var d) ? d : 0;
            var locationCost = CalculateSameDayLocationCost(lecturer.PersonKey, day, schedule, sameDayLocationMap);

            var score = 5000;
            score += Math.Max(0, 500 - load * 100);
            score += Math.Max(0, 100 - sameDayLoad * 30);
            score += GetLocationScoreBonus(locationCost);

            return score;
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
            int score,
            string reason)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var positionNo = GetNextPositionNo(assignedPositions);

            plan.NewInvigilators.Add(new AutoAssignInvigilatorCreateDto
            {
                AssigneeId = lecturer.UserId,
                AssignerId = assignerId,
                ExamScheduleId = schedule.ExamScheduleId,
                PositionNo = positionNo,
                Status = "Chưa gửi xác nhận",
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

        private static byte GetNextPositionNo(HashSet<byte> assignedPositions)
        {
            for (byte position = 1; position <= RequiredInvigilatorsPerSchedule; position++)
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
            AutoAssignmentMode assignmentMode)
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
                var plan = new AutoAssignPlanDto();
                var details = schedules.ToDictionary(
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
                        RequiredCount = RequiredInvigilatorsPerSchedule,
                        AssignedCount = scheduleAssignedUsers.TryGetValue(x.ExamScheduleId, out var assigned) ? assigned.Count : 0
                    });

                var processableSchedules = schedules
                    .Where(x => CanProcessSchedule(x, scheduleAssignedUsers))
                    .ToList();

                foreach (var schedule in schedules.Where(x => !CanProcessSchedule(x, scheduleAssignedUsers)))
                {
                    if (IsFinalStatus(schedule.Status))
                        details[schedule.ExamScheduleId] = CreateSkippedDetail(schedule);
                }

                foreach (var schedule in processableSchedules)
                {
                    var assignedUsers = scheduleAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var set)
                        ? set
                        : new HashSet<int>();
                    var need = Math.Max(0, RequiredInvigilatorsPerSchedule - assignedUsers.Count);
                    if (need == 0)
                    {
                        continue;
                    }

                    var day = DateOnly.FromDateTime(schedule.ExamDate);
                    var scheduleVars = new List<BoolVar>();

                    foreach (var lecturer in lecturers.Where(x => IsFeasibleCpSatCandidate(x, schedule, assignedUsers, busyKeySet, occupiedKeySet)))
                    {
                        var variable = model.NewBoolVar($"x_s{schedule.ExamScheduleId}_u{lecturer.UserId}");
                        variables[(schedule.ExamScheduleId, lecturer.UserId)] = variable;
                        scheduleVars.Add(variable);

                        var tier = GetCandidateTier(lecturer, schedule, subjectLecturerMap, isLecturerRoleByUser);
                        if (tier == CandidateTier.ExactOwner)
                            exactVars.Add(variable);
                        else if (tier == CandidateTier.SameSubject)
                            sameSubjectVars.Add(variable);
                        else if (tier == CandidateTier.FacultyMember)
                            facultyMemberVars.Add(variable);
                        else
                            emergencyVars.Add(variable);

                        if (IsSubjectSpecialist(tier))
                        {
                            var formatPriority = GetExamFormatPriority(schedule.ExamFormatDisplay);
                            if (formatPriority == ExamFormatPriority.Oral) oralSpecialistVars.Add(variable);
                            if (formatPriority == ExamFormatPriority.Practical) practicalSpecialistVars.Add(variable);
                        }

                        var fixedLocationCost = CalculateSameDayLocationCost(lecturer.PersonKey, day, schedule, sameDayLocationMap);
                        if (fixedLocationCost is int cost && cost > 0)
                            locationCostTerms.Add(LinearExpr.Term(variable, cost * 45));
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
                    model.Add(LinearExpr.Sum(group.Select(x => (LinearExpr)x.Value).ToArray()) <= 1);
                }

                var expectedNewAssignments = processableSchedules.Sum(x =>
                {
                    var assignedCount = scheduleAssignedUsers.TryGetValue(x.ExamScheduleId, out var assigned) ? assigned.Count : 0;
                    return Math.Max(0, RequiredInvigilatorsPerSchedule - assignedCount);
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
                    var maxLoad = currentLoad + lecturerVars.Length;
                    var loadVar = model.NewIntVar(currentLoad, maxLoad, $"load_u{lecturer.UserId}");
                    model.Add(loadVar == LinearExpr.Sum(lecturerVars.Append(LinearExpr.Constant(currentLoad)).ToArray()));

                    var deviation = model.NewIntVar(0, Math.Max(maxLoad, targetLoad) + currentLoad + 1, $"dev_u{lecturer.UserId}");
                    model.AddAbsEquality(deviation, loadVar - targetLoad);
                    fairnessTerms.Add(LinearExpr.Term(deviation, 700));
                }

                foreach (var group in variables.GroupBy(x =>
                {
                    var schedule = scheduleById[x.Key.ScheduleId];
                    return (x.Key.LecturerId, Day: DateOnly.FromDateTime(schedule.ExamDate));
                }))
                {
                    var existingDayLoad = sameDayLoadMap.TryGetValue(group.Key, out var dayLoad) ? dayLoad : 0;
                    var dayVar = model.NewIntVar(existingDayLoad, existingDayLoad + group.Count(), $"day_u{group.Key.LecturerId}_{group.Key.Day:yyyyMMdd}");
                    model.Add(dayVar == LinearExpr.Sum(group.Select(x => (LinearExpr)x.Value).Append(LinearExpr.Constant(existingDayLoad)).ToArray()));

                    var overload = model.NewIntVar(0, existingDayLoad + group.Count(), $"day_over_u{group.Key.LecturerId}_{group.Key.Day:yyyyMMdd}");
                    model.Add(overload >= dayVar - 1);
                    fairnessTerms.Add(LinearExpr.Term(overload, 600));
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
                            var distanceCost = CalculateRoomDistanceCost(firstSchedule, secondSchedule);
                            if (distanceCost == 0)
                                continue;

                            var pair = model.NewBoolVar($"loc_u{group.Key.LecturerId}_s{firstSchedule.ExamScheduleId}_s{secondSchedule.ExamScheduleId}");
                            model.AddMultiplicationEquality(pair, new IntVar[] { groupItems[i].Value, groupItems[j].Value });
                            locationCostTerms.Add(LinearExpr.Term(pair, distanceCost * 45));
                        }
                    }
                }

                var solver = new CpSolver
                {
                    StringParameters = "max_time_in_seconds:8 num_search_workers:8"
                };

                var totalShortage = AddSumVar(model, shortageVars.Select(x => (LinearExpr)x), "total_shortage", 0, processableSchedules.Count * RequiredInvigilatorsPerSchedule);
                var oralSpecialistTotal = AddSumVar(model, oralSpecialistVars.Select(x => (LinearExpr)x), "total_oral_specialist", 0, oralSpecialistVars.Count);
                var practicalSpecialistTotal = AddSumVar(model, practicalSpecialistVars.Select(x => (LinearExpr)x), "total_practical_specialist", 0, practicalSpecialistVars.Count);
                var exactTotal = AddSumVar(model, exactVars.Select(x => (LinearExpr)x), "total_exact", 0, exactVars.Count);
                var sameSubjectTotal = AddSumVar(model, sameSubjectVars.Select(x => (LinearExpr)x), "total_same_subject", 0, sameSubjectVars.Count);

                model.Minimize(totalShortage);
                var status = solver.Solve(model);
                if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                {
                    return null;
                }

                var bestShortage = (int)solver.Value(totalShortage);
                model.Add(totalShortage == bestShortage);

                model.Maximize(oralSpecialistTotal);
                status = solver.Solve(model);
                if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                    return null;

                var bestOralSpecialist = (int)solver.Value(oralSpecialistTotal);
                model.Add(oralSpecialistTotal == bestOralSpecialist);

                model.Maximize(practicalSpecialistTotal);
                status = solver.Solve(model);
                if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                    return null;

                var bestPracticalSpecialist = (int)solver.Value(practicalSpecialistTotal);
                model.Add(practicalSpecialistTotal == bestPracticalSpecialist);

                model.Maximize(exactTotal);
                status = solver.Solve(model);
                if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                    return null;

                var bestExact = (int)solver.Value(exactTotal);
                model.Add(exactTotal == bestExact);

                model.Maximize(sameSubjectTotal);
                status = solver.Solve(model);
                if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                    return null;

                var bestSameSubject = (int)solver.Value(sameSubjectTotal);
                model.Add(sameSubjectTotal == bestSameSubject);

                fairnessTerms.AddRange(emergencyVars.Select(x => LinearExpr.Term(x, 3_000)));
                fairnessTerms.AddRange(facultyMemberVars.Select(x => LinearExpr.Term(x, 8_000)));
                fairnessTerms.AddRange(locationCostTerms);
                model.Minimize(LinearExpr.Sum(fairnessTerms.ToArray()));
                status = solver.Solve(model);
                if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                    return null;

                var mutableAssignedUsers = scheduleAssignedUsers.ToDictionary(x => x.Key, x => x.Value.ToHashSet());
                var mutableAssignedPositions = scheduleAssignedPositions.ToDictionary(x => x.Key, x => x.Value.ToHashSet());
                var selectedAssignments = variables
                    .Where(x => solver.Value(x.Value) == 1)
                    .Select(x => new
                    {
                        Schedule = scheduleById[x.Key.ScheduleId],
                        Lecturer = lecturerById[x.Key.LecturerId]
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
                    var load = lecturerLoads.TryGetValue(selected.Lecturer.UserId, out var currentLoad) ? currentLoad : 0;
                    var sameDayLoad = sameDayLoadMap.TryGetValue((selected.Lecturer.PersonKey, day), out var d) ? d : 0;
                    var tier = GetCandidateTier(selected.Lecturer, selected.Schedule, subjectLecturerMap, isLecturerRoleByUser);
                    var locationCost = CalculateSameDayLocationCost(selected.Lecturer.PersonKey, day, selected.Schedule, sameDayLocationMap);
                    var score = GetCandidateScore(tier, load, sameDayLoad, locationCost);
                    var reasonParts = new[] { GetCandidateTierReason(tier), GetLocationReason(locationCost) }
                        .Where(x => !string.IsNullOrWhiteSpace(x));
                    var reason = string.Join("; ", reasonParts);

                    AssignOne(
                        plan,
                        details[selected.Schedule.ExamScheduleId],
                        selected.Schedule,
                        selected.Lecturer,
                        request.AssignerId,
                        assignedUsers,
                        assignedPositions,
                        lecturerLoads,
                        sameDayLoadMap,
                        sameDayLocationMap,
                        occupiedKeySet,
                        score,
                        reason);
                }

                foreach (var schedule in processableSchedules)
                {
                    var assignedCount = mutableAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers)
                        ? assignedUsers.Count
                        : 0;
                    var statusAfter = assignedCount >= RequiredInvigilatorsPerSchedule ? "Chờ duyệt" : "Thiếu giám thị";
                    plan.ScheduleStatuses.Add(new AutoAssignScheduleStatusUpdateDto
                    {
                        ExamScheduleId = schedule.ExamScheduleId,
                        Status = statusAfter
                    });

                    var detail = details[schedule.ExamScheduleId];
                    detail.AssignedCount = assignedCount;
                    detail.StatusAfter = statusAfter;
                    detail.Message = assignedCount >= RequiredInvigilatorsPerSchedule
                        ? (assignmentMode == AutoAssignmentMode.RepairRejected
                            ? "Đã bổ sung giám thị thay thế và đưa lịch về trạng thái chờ duyệt lại."
                            : "Đã phân công đủ 2 giám thị theo các tiêu chí ưu tiên.")
                        : $"Chưa tìm đủ giảng viên phù hợp, còn thiếu {RequiredInvigilatorsPerSchedule - assignedCount} giám thị.";
                }

                foreach (var schedule in schedules.Where(x => !processableSchedules.Any(p => p.ExamScheduleId == x.ExamScheduleId)))
                {
                    var assignedCount = mutableAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers)
                        ? assignedUsers.Count
                        : 0;
                    var detail = details[schedule.ExamScheduleId];
                    if (string.IsNullOrWhiteSpace(detail.StatusAfter))
                        detail.StatusAfter = schedule.Status;
                    detail.AssignedCount = assignedCount;
                    if (string.IsNullOrWhiteSpace(detail.Message))
                        detail.Message = assignedCount >= RequiredInvigilatorsPerSchedule
                            ? "Không gán mới."
                            : "Không gán mới.";
                }

                var result = new AutoAssignResultDto
                {
                    Success = true,
                    TotalSchedules = schedules.Count,
                    AssignedInvigilators = plan.NewInvigilators.Count,
                    FullyAssignedSchedules = details.Values.Count(x => x.StatusAfter == "Chờ duyệt"),
                    MissingSchedules = details.Values.Count(x => x.StatusAfter == "Thiếu giám thị"),
                    Details = schedules.Select(x => details[x.ExamScheduleId]).ToList(),
                    Message = status == CpSolverStatus.Optimal
                        ? (assignmentMode == AutoAssignmentMode.RepairRejected
                            ? "Đã hoàn tất bổ sung giám thị cho các lịch cần xử lý lại."
                            : "Đã hoàn tất tự động phân công giám thị.")
                        : (assignmentMode == AutoAssignmentMode.RepairRejected
                            ? "Đã bổ sung giám thị trong thời gian cho phép."
                            : "Đã tự động phân công trong thời gian cho phép.")
                };

                if (status != CpSolverStatus.Optimal)
                    result.Warnings.Add("Hệ thống đã chọn phương án phù hợp trong thời gian giới hạn.");
                if (result.MissingSchedules > 0)
                    result.Warnings.Add("Một số lịch vẫn chưa đủ 2 giám thị do không còn giảng viên phù hợp theo lịch bận và trạng thái hiện tại.");

                return new CpSatAssignmentResult(plan, result);
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
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var busyKey = (lecturer.UserId, schedule.SlotId, day);
            var occupiedKey = (lecturer.PersonKey, schedule.SlotId, day);

            return lecturer.IsActive
                   && !assignedUsers.Contains(lecturer.PersonKey)
                   && !busyKeySet.Contains(busyKey)
                   && !occupiedKeySet.Contains(occupiedKey);
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
            Dictionary<int, HashSet<int>> scheduleAssignedUsers)
        {
            if (schedule.Status.Equals("Từ chối duyệt", StringComparison.OrdinalIgnoreCase))
                return false;

            var assignedCount = scheduleAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers)
                ? assignedUsers.Count
                : 0;
            return assignedCount < RequiredInvigilatorsPerSchedule;
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

        private static int GetCandidateScore(CandidateTier tier, int load, int sameDayLoad, int? locationCost)
        {
            var baseScore = tier switch
            {
                CandidateTier.ExactOwner => 12_000,
                CandidateTier.SameSubject => 8_000,
                CandidateTier.Emergency => 3_000,
                _ => 1_000
            };

            return Math.Max(0, baseScore - load * 120 - sameDayLoad * 120 + GetLocationScoreBonus(locationCost));
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

        private static string GetCandidateTierReason(CandidateTier tier)
        {
            return tier switch
            {
                CandidateTier.ExactOwner => "Đang dạy lớp học phần",
                CandidateTier.SameSubject => "Có chuyên môn môn thi",
                CandidateTier.Emergency => "Phù hợp lịch",
                _ => "Dự phòng khi thiếu giảng viên"
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
            string Reason);

        private sealed record CpSatAssignmentResult(
            AutoAssignPlanDto Plan,
            AutoAssignResultDto Result);

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
