using System.Diagnostics;
using System.Globalization;
using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Common.Constants;
using Google.OrTools.Sat;

namespace ExamInvigilationManagement.Application.Services
{
    public partial class AutoAssignmentService
    {
        private static CpSatAssignmentResult? TryBuildCpSatPlan(
            AutoAssignRequestDto request,
            List<AutoAssignScheduleDto> schedules,
            List<AutoAssignLecturerDto> lecturers,
            Dictionary<int, int> lecturerLoads,
            HashSet<(int UserId, int SlotId, DateOnly BusyDate)> busyKeySet,
            HashSet<(int PersonKey, int SlotId, DateOnly BusyDate)> occupiedKeySet,
            Dictionary<int, HashSet<int>> scheduleAssignedUsers,
            Dictionary<int, HashSet<byte>> scheduleAssignedPositions,
            Dictionary<int, HashSet<int>> blockedPreviousAssigneesBySchedule,
            IReadOnlyList<int> cancelledExistingInvigilatorIds,
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
                        blockedPreviousAssigneesBySchedule,
                        policy)))
                    {
                        var tier = GetCandidateTier(lecturer, schedule, subjectLecturerMap, isLecturerRoleByUser);
                        if (IsOwnerOnly(schedule, policy) && lecturer.PersonKey != schedule.OfferingUserPersonKey)
                            continue;
                        if (tier == CandidateTier.FacultyMember && !policy.AllowFacultyMemberAsFallback)
                            continue;

                        var variable = model.NewBoolVar($"x_s{schedule.ExamScheduleId}_l{lecturer.UserId}");
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
                    var loadVar = model.NewIntVar(currentLoad, maxLoad, $"load_l{lecturer.UserId}");

                    model.Add(loadVar == LinearExpr.Sum(lecturerVars.Append(LinearExpr.Constant(currentLoad)).ToArray()));

                    var deviation = model.NewIntVar(0, Math.Max(maxLoad, targetLoad) + currentLoad + 1, $"dev_l{lecturer.UserId}");
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

                    var dayVar = model.NewIntVar(existingDayLoad, existingDayLoad + group.Count(), $"dayLoad_l{group.Key.LecturerId}_{group.Key.Day:yyyyMMdd}");
                    model.Add(dayVar == LinearExpr.Sum(group.Select(x => (LinearExpr)x.Value).Append(LinearExpr.Constant(existingDayLoad)).ToArray()));

                    var overload = model.NewIntVar(0, existingDayLoad + group.Count(), $"day_over_l{group.Key.LecturerId}_{group.Key.Day:yyyyMMdd}");
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

                            var pair = model.NewBoolVar($"loc_l{group.Key.LecturerId}_s{firstSchedule.ExamScheduleId}_s{secondSchedule.ExamScheduleId}");
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
                    resultPlan.CancelledExistingInvigilatorIds.AddRange(cancelledExistingInvigilatorIds);
                    var resultDetails = schedules.ToDictionary(
                        x => x.ExamScheduleId,
                        x => new AutoAssignScheduleResultDto
                        {
                            ExamScheduleId = x.ExamScheduleId,
                            ExamDate = x.ExamDate,
                            SessionName = x.SessionName,
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
            Dictionary<int, HashSet<int>> blockedPreviousAssigneesBySchedule,
            AutoAssignmentPolicyDto policy)
        {
            var day = DateOnly.FromDateTime(schedule.ExamDate);
            var busyKey = (lecturer.UserId, schedule.SlotId, day);
            var occupiedKey = (lecturer.PersonKey, schedule.SlotId, day);
            var isPreviouslyRejectedAssignee = blockedPreviousAssigneesBySchedule.TryGetValue(schedule.ExamScheduleId, out var blockedAssignees) &&
                                               blockedAssignees.Contains(lecturer.PersonKey);

            if (!lecturer.IsActive ||
                assignedUsers.Contains(lecturer.PersonKey) ||
                isPreviouslyRejectedAssignee ||
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

        private sealed record CpSatAssignmentResult(
            AutoAssignPlanDto Plan,
            AutoAssignResultDto Result);
    }
}
