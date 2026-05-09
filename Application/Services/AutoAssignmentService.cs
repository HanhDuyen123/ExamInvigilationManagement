using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;

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
                    request.SemesterId,
                    request.PeriodId,
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
                request.SemesterId,
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

            var occupiedKeySet = new HashSet<(int UserId, int SlotId, DateOnly BusyDate)>();
            foreach (var x in existingAssignments)
                occupiedKeySet.Add((x.UserId, x.SlotId, DateOnly.FromDateTime(x.ExamDate)));

            var scheduleAssignedUsers = existingAssignments
                .GroupBy(x => x.ExamScheduleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.UserId).ToHashSet());

            var sameDayLoadMap = existingAssignments
                .GroupBy(x => (x.UserId, DateOnly.FromDateTime(x.ExamDate)))
                .ToDictionary(g => g.Key, g => g.Count());

            var ownerScheduleCountByLecturer = schedules
                .GroupBy(x => x.OfferingUserId)
                .ToDictionary(g => g.Key, g => g.Count());

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

                scheduleAssignedUsers[schedule.ExamScheduleId] = assignedUsers;

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
            if (request.SemesterId <= 0)
                throw new ArgumentException("SemesterId không hợp lệ.");

            if (request.PeriodId <= 0)
                throw new ArgumentException("PeriodId không hợp lệ.");

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
            DateOnly day)
        {
            var candidates = lecturers
                .Where(l => IsFeasibleFallback(l, schedule, assignedUsers, busyKeySet, occupiedKeySet))
                .Select(l =>
                {
                    var load = lecturerLoads.TryGetValue(l.UserId, out var currentLoad) ? currentLoad : 0;
                    var sameDayLoad = sameDayLoadMap.TryGetValue((l.UserId, day), out var d) ? d : 0;
                    var ownerCount = ownerScheduleCountByLecturer.TryGetValue(l.UserId, out var c) ? c : 0;

                    var score = 0;
                    var reasons = new List<string>();

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
            Dictionary<int, int> lecturerLoads,
            Dictionary<(int UserId, DateOnly Day), int> sameDayLoadMap,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            int score,
            string reason)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);

            plan.NewInvigilators.Add(new AutoAssignInvigilatorCreateDto
            {
                AssigneeId = lecturer.UserId,
                AssignerId = assignerId,
                ExamScheduleId = schedule.ExamScheduleId,
                PositionNo = (byte)(assignedUsers.Count + 1),
                Status = "chờ",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });

            assignedUsers.Add(lecturer.UserId);
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
                PositionNo = (byte)assignedUsers.Count,
                Score = score,
                Reason = reason
            });
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
    }
}