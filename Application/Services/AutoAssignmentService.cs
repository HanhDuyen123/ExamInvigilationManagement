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
            ValidateRequest(request);

            var facultyId = await _repository.GetUserFacultyIdAsync(request.AssignerId, cancellationToken);
            if (facultyId is null || facultyId <= 0)
                throw new ArgumentException("Không xác định được khoa của người thực hiện auto assignment.");

            var lecturers = (await _repository.GetActiveLecturersAsync(facultyId.Value, cancellationToken))
                .ToList();

            if (lecturers.Count == 0)
                throw new InvalidOperationException("Không có giảng viên hợp lệ trong khoa để phân công.");

            var schedules = (await _repository.GetSchedulesAsync(
                    request.SemesterId!.Value,
                    request.PeriodId!.Value,
                    facultyId.Value,
                    cancellationToken))
                .ToList();

            var result = new AutoAssignResultDto
            {
                TotalSchedules = schedules.Count
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
                facultyId.Value,
                cancellationToken);

            var subjectLecturerMap = await _repository.GetSubjectLecturerMapAsync(
                schedules.Select(x => x.SubjectId),
                facultyId.Value,
                cancellationToken);

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

            var occupiedKeySet = new HashSet<(int UserId, int SlotId, DateOnly BusyDate)>();
            foreach (var x in activeExistingAssignments)
                occupiedKeySet.Add((x.UserId, x.SlotId, DateOnly.FromDateTime(x.ExamDate)));

            var scheduleAssignedUsers = activeExistingAssignments
                .GroupBy(x => x.ExamScheduleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.UserId).ToHashSet());

            var scheduleAssignedPositions = activeExistingAssignments
                .GroupBy(x => x.ExamScheduleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.PositionNo).ToHashSet());

            var sameDayLoadMap = activeExistingAssignments
                .GroupBy(x => (x.UserId, DateOnly.FromDateTime(x.ExamDate)))
                .ToDictionary(g => g.Key, g => g.Count());

            var ownerScheduleCountByLecturer = schedules
                .GroupBy(x => x.OfferingUserId)
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
                subjectLecturerMap,
                assignmentMode);

            if (solverResult != null)
            {
                await _repository.SavePlanAsync(solverResult.Plan, cancellationToken);
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

                if (assignedUsers.Contains(schedule.OfferingUserId))
                    continue;

                var exactOwner = lecturers.FirstOrDefault(x =>
                    x.UserId == schedule.OfferingUserId &&
                    IsFeasibleExactOwner(x, schedule, assignedUsers, busyKeySet, occupiedKeySet));

                if (exactOwner != null)
                {
                    var score = CalculateExactOwnerScore(
                        exactOwner,
                        schedule,
                        lecturerLoads,
                        sameDayLoadMap,
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
                        busyKeySet,
                        occupiedKeySet,
                        ownerScheduleCountByLecturer,
                        subjectLecturerMap,
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

            await _repository.SavePlanAsync(plan, cancellationToken);

            result.AssignedInvigilators = plan.NewInvigilators.Count;
            result.FullyAssignedSchedules = result.Details.Count(x => x.StatusAfter == "Chờ duyệt");
            result.MissingSchedules = result.Details.Count(x => x.StatusAfter == "Thiếu giám thị");
            result.Success = true;
            result.Message = result.MissingSchedules > 0
                ? "Auto assignment hoàn thành nhưng còn một số lịch thiếu giám thị."
                : "Auto assignment hoàn thành.";

            if (result.MissingSchedules > 0)
                result.Warnings.Add("Một số lịch không đủ 2 giám thị trong phạm vi cùng khoa.");

            return result;
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
                StatusBefore = schedule.Status,
                StatusAfter = schedule.Status,
                RequiredCount = RequiredInvigilatorsPerSchedule,
                AssignedCount = 0,
                Message = "Bỏ qua vì lịch thi đã ở trạng thái cuối."
            };
        }

        private static (int TotalAvailableCandidates, bool HasExactOwner) CalculateScheduleDifficulty(
            AutoAssignScheduleDto schedule,
            List<AutoAssignLecturerDto> lecturers,
            Dictionary<int, HashSet<int>> scheduleAssignedUsers,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> occupiedKeySet)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);

            if (!scheduleAssignedUsers.TryGetValue(schedule.ExamScheduleId, out var assignedUsers))
                assignedUsers = new HashSet<int>();

            var hasExactOwner = lecturers.Any(l =>
                l.UserId == schedule.OfferingUserId &&
                IsFeasibleExactOwner(l, schedule, assignedUsers, busyKeySet, occupiedKeySet));

            var fallbackCandidates = lecturers.Count(l =>
                l.IsActive &&
                l.UserId != schedule.OfferingUserId &&
                !assignedUsers.Contains(l.UserId) &&
                !busyKeySet.Contains((l.UserId, schedule.SlotId, day)) &&
                !occupiedKeySet.Contains((l.UserId, schedule.SlotId, day)));

            return (fallbackCandidates + (hasExactOwner ? 1 : 0), hasExactOwner);
        }

        private static bool IsFeasibleExactOwner(
            AutoAssignLecturerDto lecturer,
            AutoAssignScheduleDto schedule,
            HashSet<int> assignedUsers,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> occupiedKeySet)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var key = (lecturer.UserId, schedule.SlotId, day);

            return lecturer.IsActive
                   && lecturer.UserId == schedule.OfferingUserId
                   && !assignedUsers.Contains(lecturer.UserId)
                   && !busyKeySet.Contains(key)
                   && !occupiedKeySet.Contains(key);
        }

        private static FallbackCandidate? PickBestFallbackCandidate(
            AutoAssignScheduleDto schedule,
            List<AutoAssignLecturerDto> lecturers,
            HashSet<int> assignedUsers,
            Dictionary<int, int> lecturerLoads,
            Dictionary<(int UserId, DateOnly Day), int> sameDayLoadMap,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            Dictionary<int, int> ownerScheduleCountByLecturer,
            Dictionary<string, HashSet<int>> subjectLecturerMap,
            DateOnly day)
        {
            var candidates = lecturers
                .Where(l => IsFeasibleFallback(l, schedule, assignedUsers, busyKeySet, occupiedKeySet))
                .Select(l =>
                {
                    var load = lecturerLoads.TryGetValue(l.UserId, out var currentLoad) ? currentLoad : 0;
                    var sameDayLoad = sameDayLoadMap.TryGetValue((l.UserId, day), out var d) ? d : 0;
                    var ownerCount = ownerScheduleCountByLecturer.TryGetValue(l.UserId, out var c) ? c : 0;
                    var tier = GetCandidateTier(l.UserId, schedule, subjectLecturerMap);

                    var score = 0;
                    var reasons = new List<string>();

                    if (tier == CandidateTier.SameSubject)
                    {
                        score += 2500;
                        reasons.Add("từng dạy cùng môn");
                    }
                    else
                    {
                        score -= 2500;
                        reasons.Add("giảng viên cùng khoa phù hợp lịch");
                    }

                    // Ưu tiên người ít tải
                    score += Math.Max(0, 1000 - load * 120);
                    reasons.Add($"tải hiện tại: {load}");

                    // Ưu tiên ít ca trong ngày
                    score += Math.Max(0, 120 - sameDayLoad * 40);
                    reasons.Add($"ca trong ngày: {sameDayLoad}");

                    // Phạt nhẹ nếu người này là owner của nhiều lịch khác trong batch
                    // để tránh làm họ bị “ăn mất” quá nhiều, nhưng không loại bỏ hoàn toàn
                    score -= ownerCount * 150;
                    reasons.Add($"số lịch chủ lớp: {ownerCount}");

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
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> occupiedKeySet)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var key = (lecturer.UserId, schedule.SlotId, day);

            // Chỉ cấm chính owner của lịch hiện tại.
            // Không cấm toàn bộ lecturer đang là owner của lịch khác,
            // vì pha 1 đã reserve owner rồi.
            return lecturer.IsActive
                   && lecturer.UserId != schedule.OfferingUserId
                   && !assignedUsers.Contains(lecturer.UserId)
                   && !busyKeySet.Contains(key)
                   && !occupiedKeySet.Contains(key);
        }

        private static int CalculateExactOwnerScore(
            AutoAssignLecturerDto lecturer,
            AutoAssignScheduleDto schedule,
            Dictionary<int, int> lecturerLoads,
            Dictionary<(int UserId, DateOnly Day), int> sameDayLoadMap,
            DateOnly day)
        {
            var load = lecturerLoads.TryGetValue(lecturer.UserId, out var currentLoad) ? currentLoad : 0;
            var sameDayLoad = sameDayLoadMap.TryGetValue((lecturer.UserId, day), out var d) ? d : 0;

            var score = 5000;
            score += Math.Max(0, 500 - load * 100);
            score += Math.Max(0, 100 - sameDayLoad * 30);

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
            Dictionary<(int UserId, DateOnly Day), int> sameDayLoadMap,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> occupiedKeySet,
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
                Status = "Chờ xác nhận",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });

            assignedUsers.Add(lecturer.UserId);
            assignedPositions.Add(positionNo);
            occupiedKeySet.Add((lecturer.UserId, schedule.SlotId, day));

            lecturerLoads[lecturer.UserId] = lecturerLoads.TryGetValue(lecturer.UserId, out var load)
                ? load + 1
                : 1;

            var sameDayKey = (lecturer.UserId, day);
            sameDayLoadMap[sameDayKey] = sameDayLoadMap.TryGetValue(sameDayKey, out var dayLoad)
                ? dayLoad + 1
                : 1;

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
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            Dictionary<int, HashSet<int>> scheduleAssignedUsers,
            Dictionary<int, HashSet<byte>> scheduleAssignedPositions,
            Dictionary<(int UserId, DateOnly Day), int> sameDayLoadMap,
            Dictionary<string, HashSet<int>> subjectLecturerMap,
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
                var emergencyVars = new List<BoolVar>();
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

                        var tier = GetCandidateTier(lecturer.UserId, schedule, subjectLecturerMap);
                        if (tier == CandidateTier.ExactOwner)
                            exactVars.Add(variable);
                        else if (tier == CandidateTier.SameSubject)
                            sameSubjectVars.Add(variable);
                        else
                            emergencyVars.Add(variable);
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

                var solver = new CpSolver
                {
                    StringParameters = "max_time_in_seconds:8 num_search_workers:8"
                };

                var totalShortage = AddSumVar(model, shortageVars.Select(x => (LinearExpr)x), "total_shortage", 0, processableSchedules.Count * RequiredInvigilatorsPerSchedule);
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
                    .OrderBy(x => GetCandidateTier(x.Lecturer.UserId, x.Schedule, subjectLecturerMap))
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
                    var sameDayLoad = sameDayLoadMap.TryGetValue((selected.Lecturer.UserId, day), out var d) ? d : 0;
                    var tier = GetCandidateTier(selected.Lecturer.UserId, selected.Schedule, subjectLecturerMap);
                    var score = GetCandidateScore(tier, load, sameDayLoad);
                    var reason = $"{GetCandidateTierReason(tier)}; tải hiện tại: {load}; số lịch trong ngày: {sameDayLoad}";

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
                            ? "Giữ nguyên vì lịch đã đủ giám thị hợp lệ theo state-aware filtering."
                            : "Bỏ qua vì trạng thái lịch không cho phép tự động phân công.";
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
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> occupiedKeySet)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var key = (lecturer.UserId, schedule.SlotId, day);

            return lecturer.IsActive
                   && !assignedUsers.Contains(lecturer.UserId)
                   && !busyKeySet.Contains(key)
                   && !occupiedKeySet.Contains(key);
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
            int lecturerId,
            AutoAssignScheduleDto schedule,
            Dictionary<string, HashSet<int>> subjectLecturerMap)
        {
            if (lecturerId == schedule.OfferingUserId)
                return CandidateTier.ExactOwner;

            return subjectLecturerMap.TryGetValue(schedule.SubjectId, out var lecturerIds) && lecturerIds.Contains(lecturerId)
                ? CandidateTier.SameSubject
                : CandidateTier.Emergency;
        }

        private static int GetCandidateBaseCost(CandidateTier tier)
        {
            return tier switch
            {
                CandidateTier.ExactOwner => -50_000,
                CandidateTier.SameSubject => 1_000,
                _ => 12_000
            };
        }

        private static int GetCandidateScore(CandidateTier tier, int load, int sameDayLoad)
        {
            var baseScore = tier switch
            {
                CandidateTier.ExactOwner => 12_000,
                CandidateTier.SameSubject => 8_000,
                _ => 3_000
            };

            return Math.Max(0, baseScore - load * 120 - sameDayLoad * 120);
        }

        private static string GetCandidateTierReason(CandidateTier tier)
        {
            return tier switch
            {
                CandidateTier.ExactOwner => "đúng giảng viên phụ trách lớp học phần",
                CandidateTier.SameSubject => "giảng viên từng dạy cùng môn",
                _ => "giảng viên cùng khoa phù hợp lịch"
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

        private enum CandidateTier
        {
            ExactOwner = 0,
            SameSubject = 1,
            Emergency = 2
        }

        private enum AutoAssignmentMode
        {
            InitialAssignment = 0,
            RepairRejected = 1
        }
    }
}
