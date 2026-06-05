using ExamInvigilationManagement.Application.DTOs.ManualAssignment;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;

namespace ExamInvigilationManagement.Application.Services
{
    public class ManualAssignmentService : IManualAssignmentService
    {
        private const int RequiredInvigilatorsPerSchedule = 2;

        private readonly IManualAssignmentRepository _repository;

        public ManualAssignmentService(IManualAssignmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<ManualAssignmentPageDto> GetPageAsync(
            int scheduleId,
            int assignerId,
            CancellationToken cancellationToken = default)
        {
            if (scheduleId <= 0)
                throw new ArgumentException("ScheduleId không hợp lệ.");

            var facultyId = await _repository.GetUserFacultyIdAsync(assignerId, cancellationToken);
            if (facultyId is null || facultyId <= 0)
                throw new InvalidOperationException("Không xác định được khoa của người thực hiện.");

            var schedule = await _repository.GetScheduleAsync(scheduleId, facultyId.Value, cancellationToken);
            if (schedule is null)
                throw new InvalidOperationException("Không tìm thấy lịch thi thuộc khoa của bạn.");

            var currentInvigilators = await _repository.GetCurrentInvigilatorsAsync(scheduleId, cancellationToken);
            var activityLogs = await _repository.GetActivityLogsAsync(scheduleId, cancellationToken);
            var lecturers = await _repository.GetActiveLecturersAsync(
                facultyId.Value,
                schedule.SubjectId,
                schedule.OfferingUserId,
                cancellationToken);

            var lecturerIds = lecturers.Select(x => x.UserId).ToList();
            var whitelistedLecturerIds = await _repository.GetPeriodAvailableLecturerIdsAsync(
                schedule.PeriodId,
                facultyId.Value,
                lecturerIds,
                cancellationToken);
            if (whitelistedLecturerIds.Count > 0 || await _repository.HasPeriodAvailabilityListAsync(schedule.PeriodId, facultyId.Value, cancellationToken))
            {
                lecturers = lecturers.Where(x => whitelistedLecturerIds.Contains(x.UserId)).ToList();
                lecturerIds = lecturers.Select(x => x.UserId).ToList();
            }

            var busyWholePeriodIds = await _repository.GetApprovedBusyPeriodLecturerIdsAsync(
                schedule.PeriodId,
                lecturerIds,
                cancellationToken);
            if (busyWholePeriodIds.Count > 0)
            {
                lecturers = lecturers.Where(x => !busyWholePeriodIds.Contains(x.UserId)).ToList();
                lecturerIds = lecturers.Select(x => x.UserId).ToList();
            }

            var examDateOnly = DateOnly.FromDateTime(schedule.ExamDate);

            var busyIds = await _repository.GetBusyLecturerIdsAsync(
                lecturerIds,
                schedule.SlotId,
                examDateOnly,
                cancellationToken);

            var conflictIds = await _repository.GetConflictingLecturerIdsAsync(
                scheduleId,
                schedule.SlotId,
                schedule.ExamDate,
                lecturerIds,
                cancellationToken);

            var loads = await _repository.GetLecturerLoadsAsync(
                schedule.SemesterId,
                lecturerIds,
                cancellationToken);

            var sameDayLoads = await _repository.GetSameDayLoadsAsync(
                schedule.SemesterId,
                lecturerIds,
                schedule.ExamDate,
                cancellationToken);

            var subjectLecturerIds = await _repository.GetSubjectLecturerIdsAsync(schedule.SubjectId, cancellationToken);

            var replaceablePositions = currentInvigilators
                .Where(x => !x.HasReplacement && string.Equals(x.ResponseStatus, "Từ chối", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.PositionNo)
                .ToHashSet();

            var currentPersonKeys = currentInvigilators.Select(x => x.EffectivePersonKey).ToHashSet();

            var options = lecturers
                .Select(x =>
                {
                    var isExactOwner = x.UserId == schedule.OfferingUserId || x.PersonKey == schedule.OfferingUserPersonKey;
                    var hasTaughtSubject = subjectLecturerIds.Contains(x.UserId) || subjectLecturerIds.Contains(x.PersonKey);
                    var isBusy = busyIds.Contains(x.UserId);
                    var isConflict = conflictIds.Contains(x.UserId);
                    var isAlreadyAssigned = currentPersonKeys.Contains(x.PersonKey);

                    var currentLoad = loads.TryGetValue(x.UserId, out var l) ? l : 0;
                    var sameDayLoad = sameDayLoads.TryGetValue(x.UserId, out var d) ? d : 0;

                    var canSelect = !isBusy && !isConflict && !isAlreadyAssigned;

                    var score = 0;
                    var formatPriority = GetExamFormatPriority(schedule.ExamFormatDisplay);
                    if (isExactOwner) score += formatPriority == ExamFormatPriority.Oral ? 1800 : formatPriority == ExamFormatPriority.Practical ? 1500 : 1000;
                    else if (hasTaughtSubject) score += formatPriority == ExamFormatPriority.Oral ? 1600 : formatPriority == ExamFormatPriority.Practical ? 1300 : 700;
                    if (x.IsLecturerRole) score += 300;
                    score += Math.Max(0, 200 - currentLoad * 20);
                    score += Math.Max(0, 80 - sameDayLoad * 20);

                    var reasons = new List<string>();
                    reasons.Add(x.IsLecturerRole ? "Giảng viên" : $"Nhân sự hỗ trợ ({x.RoleName})");
                    if (isExactOwner) reasons.Add("Đang dạy lớp học phần này");
                    else if (hasTaughtSubject) reasons.Add("Đã hoặc đang dạy môn này");
                    if (formatPriority == ExamFormatPriority.Oral) reasons.Add("Hình thức vấn đáp cần ưu tiên người có chuyên môn");
                    else if (formatPriority == ExamFormatPriority.Practical) reasons.Add("Hình thức thi/chấm tại chỗ nên ưu tiên người có chuyên môn");
                    if (currentLoad == 0) reasons.Add("Chưa có lịch coi thi nào trong học kỳ");
                    else reasons.Add($"Đã có {currentLoad} lịch coi thi trong học kỳ");
                    if (sameDayLoad == 0) reasons.Add("Chưa có lịch coi thi trong ngày này");
                    else reasons.Add($"Đã có {sameDayLoad} lịch coi thi trong cùng ngày");
                    if (isBusy) reasons.Add("Giảng viên đã đăng ký bận vào ca này");
                    if (isConflict) reasons.Add("Đã có lịch coi thi khác trùng ngày và ca");
                    if (isAlreadyAssigned) reasons.Add("Đã được phân công cho lịch này");

                    return new ManualAssignmentLecturerOptionDto
                    {
                        UserId = x.UserId,
                        InformationId = x.InformationId,
                        UserName = x.UserName,
                        FullName = x.FullName,
                        FacultyId = x.FacultyId,
                        FacultyName = x.FacultyName,
                        RoleName = x.RoleName,
                        IsLecturerRole = x.IsLecturerRole,
                        CurrentLoad = currentLoad,
                        SameDayLoad = sameDayLoad,
                        IsExactOwner = isExactOwner,
                        HasTaughtSubject = hasTaughtSubject,
                        CanSelect = canSelect,
                        PriorityScore = score,
                        Reason = string.Join("; ", reasons),
                        RecommendationLabel = BuildRecommendationLabel(isExactOwner, hasTaughtSubject, x.IsLecturerRole, currentLoad, sameDayLoad, canSelect, formatPriority),
                        WorkloadLabel = BuildWorkloadLabel(currentLoad, sameDayLoad),
                        AvailabilityLabel = BuildAvailabilityLabel(isBusy, isConflict, isAlreadyAssigned)
                    };
                })
                .OrderByDescending(x => x.CanSelect)
                .ThenByDescending(x => x.IsExactOwner)
                .ThenByDescending(x => x.HasTaughtSubject)
                .ThenByDescending(x => x.PriorityScore)
                .ThenByDescending(x => x.IsLecturerRole)
                .ThenBy(x => x.CurrentLoad)
                .ThenBy(x => x.SameDayLoad)
                .ThenBy(x => x.FullName)
                .ToList();

            var missingPositions = new List<byte>();
            if (!currentInvigilators.Any(x => x.PositionNo == 1) || replaceablePositions.Contains(1)) missingPositions.Add(1);
            if (!currentInvigilators.Any(x => x.PositionNo == 2) || replaceablePositions.Contains(2)) missingPositions.Add(2);
            var effectiveInvigilatorCount = currentInvigilators.Count - replaceablePositions.Count;

            return new ManualAssignmentPageDto
            {
                Schedule = new ManualAssignmentScheduleDto
                {
                    ExamScheduleId = schedule.ExamScheduleId,
                    SlotId = schedule.SlotId,
                    SlotName = schedule.SlotName,
                    TimeStart = schedule.TimeStart,
                    AcademyYearId = schedule.AcademyYearId,
                    AcademyYearName = schedule.AcademyYearName,
                    SemesterId = schedule.SemesterId,
                    SemesterName = schedule.SemesterName,
                    PeriodId = schedule.PeriodId,
                    PeriodName = schedule.PeriodName,
                    SessionId = schedule.SessionId,
                    SessionName = schedule.SessionName,
                    RoomId = schedule.RoomId,
                    RoomDisplay = schedule.RoomDisplay,
                    OfferingId = schedule.OfferingId,
                    OfferingUserId = schedule.OfferingUserId,
                    OfferingUserInformationId = schedule.OfferingUserInformationId,
                    OfferingUserName = schedule.OfferingUserName,
                    OfferingUserFullName = schedule.OfferingUserFullName,
                    OfferingFacultyId = schedule.OfferingFacultyId,
                    SubjectId = schedule.SubjectId,
                    SubjectName = schedule.SubjectName,
                    ClassName = schedule.ClassName,
                    GroupNumber = schedule.GroupNumber,
                    ExamFormatDisplay = schedule.ExamFormatDisplay,
                    ExamDate = schedule.ExamDate,
                    Status = schedule.Status,
                    CurrentInvigilatorCount = effectiveInvigilatorCount,
                    MissingCount = Math.Max(0, RequiredInvigilatorsPerSchedule - effectiveInvigilatorCount),
                    CanEdit = (IsEditableStatus(schedule.Status) && currentInvigilators.Count < RequiredInvigilatorsPerSchedule) || replaceablePositions.Any(),
                    EditReason = BuildEditReason(schedule.Status, currentInvigilators.Count, replaceablePositions.Any())
                },
                CurrentInvigilators = currentInvigilators,
                LecturerOptions = options,
                ActivityLogs = activityLogs,
                MissingPositions = missingPositions,
                Request = new ManualAssignmentRequestDto
                {
                    ExamScheduleId = schedule.ExamScheduleId,
                    AssignerId = assignerId
                }
            };
        }

        public async Task<ManualAssignmentResultDto> AssignAsync(
    ManualAssignmentRequestDto request,
    CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.ExamScheduleId <= 0)
                errors.Add("Lịch thi không hợp lệ.");

            if (request.AssignerId <= 0)
                errors.Add("Không xác định được người thực hiện.");

            if (errors.Count > 0)
            {
                return new ManualAssignmentResultDto
                {
                    Success = false,
                    Message = "Dữ liệu đầu vào không hợp lệ.",
                    Errors = errors,
                    ExamScheduleId = request.ExamScheduleId
                };
            }

            // Dùng chung nguồn dữ liệu với màn hình GET
            var page = await GetPageAsync(request.ExamScheduleId, request.AssignerId, cancellationToken);
            var schedule = page.Schedule;

            if (!schedule.CanEdit)
            {
                return Fail(schedule.ExamScheduleId, schedule.EditReason);
            }

            var currentInvigilators = page.CurrentInvigilators;
            var lecturerOptions = page.LecturerOptions.ToDictionary(x => x.UserId);

            var currentPositions = currentInvigilators.Select(x => x.PositionNo).ToHashSet();
            var replaceableByPosition = currentInvigilators
                .Where(x => !x.HasReplacement && string.Equals(x.ResponseStatus, "Từ chối", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.PositionNo);
            var currentPersonKeys = currentInvigilators.Select(x => x.EffectivePersonKey).ToHashSet();

            var selectedByPosition = new Dictionary<byte, int?>
            {
                [1] = request.Position1AssigneeId,
                [2] = request.Position2AssigneeId
            };

            var selectedAssignments = selectedByPosition
                .Where(x => !currentPositions.Contains(x.Key) && x.Value.HasValue)
                .ToDictionary(x => x.Key, x => x.Value!.Value);

            var selectedReplacements = selectedByPosition
                .Where(x => currentPositions.Contains(x.Key) && replaceableByPosition.ContainsKey(x.Key) && x.Value.HasValue)
                .ToDictionary(x => x.Key, x => x.Value!.Value);

            if (selectedAssignments.Count == 0 && selectedReplacements.Count == 0)
            {
                return Fail(schedule.ExamScheduleId, "Vui lòng chọn ít nhất 1 giảng viên để phân công hoặc thay thế vị trí đã từ chối.");
            }

            // Không cho sửa vị trí đã tồn tại
            foreach (var position in currentPositions)
            {
                if (!replaceableByPosition.ContainsKey(position) && selectedByPosition.TryGetValue(position, out var selectedId) && selectedId.HasValue)
                    errors.Add($"Vị trí {position} đã có giám thị, không được thay đổi tại màn hình này.");
            }

            // Không được chọn cùng 1 giảng viên cho 2 vị trí
            var selectedUserIds = selectedAssignments.Values.Concat(selectedReplacements.Values).ToList();
            if (selectedUserIds.Distinct().Count() != selectedUserIds.Count)
                errors.Add("Không được chọn cùng một giảng viên cho 2 vị trí khác nhau.");

            var selectedPersonKeys = selectedUserIds
                .Where(lecturerOptions.ContainsKey)
                .Select(x => lecturerOptions[x].PersonKey)
                .ToList();
            if (selectedPersonKeys.Distinct().Count() != selectedPersonKeys.Count)
                errors.Add("Không được chọn cùng một người cho 2 vị trí khác nhau, kể cả khi người đó có nhiều tài khoản/role.");

            foreach (var kvp in selectedAssignments.Concat(selectedReplacements))
            {
                var position = kvp.Key;
                var lecturerId = kvp.Value;

                if (!lecturerOptions.TryGetValue(lecturerId, out var lecturer))
                {
                    errors.Add($"Giảng viên ở vị trí {position} không hợp lệ.");
                    continue;
                }

                // Dùng đúng option đã được tính toán từ GetPageAsync
                if (!lecturer.CanSelect)
                    errors.Add($"Giảng viên '{lecturer.FullName}' không thể phân công: {lecturer.Reason}");

                if (currentPersonKeys.Contains(lecturer.PersonKey))
                    errors.Add($"'{lecturer.FullName}' đã được phân công cho lịch này bằng một tài khoản/role khác.");
            }

            if (errors.Count > 0)
            {
                return new ManualAssignmentResultDto
                {
                    Success = false,
                    Message = "Không thể lưu do dữ liệu chưa hợp lệ.",
                    Errors = errors.Distinct().ToList(),
                    ExamScheduleId = schedule.ExamScheduleId
                };
            }

            var effectiveCurrentCount = currentInvigilators.Count - replaceableByPosition.Count;
            var totalAfterSave = effectiveCurrentCount + selectedAssignments.Count + selectedReplacements.Count;

            var plan = new ManualAssignmentSavePlanDto
            {
                ExamScheduleId = schedule.ExamScheduleId,
                StatusAfter = totalAfterSave >= RequiredInvigilatorsPerSchedule
                    ? "Chờ duyệt"
                    : "Thiếu giám thị"
            };

            foreach (var kvp in selectedAssignments)
            {
                plan.NewInvigilators.Add(new ManualAssignmentInvigilatorCreateDto
                {
                    AssigneeId = kvp.Value,
                    AssignerId = request.AssignerId,
                    ExamScheduleId = schedule.ExamScheduleId,
                    PositionNo = kvp.Key,
                    Status = "Chưa gửi xác nhận",
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now
                });
            }

            foreach (var kvp in selectedReplacements)
            {
                plan.ReplaceInvigilators.Add(new ManualAssignmentInvigilatorReplaceDto
                {
                    ExamInvigilatorId = replaceableByPosition[kvp.Key].ExamInvigilatorId,
                    NewAssigneeId = kvp.Value,
                    AssignerId = request.AssignerId,
                    UpdateAt = DateTime.Now
                });
            }

            await _repository.SaveAsync(plan, cancellationToken);

            return new ManualAssignmentResultDto
            {
                Success = true,
                Message = plan.StatusAfter == "Chờ duyệt"
                    ? "Phân công thủ công thành công. Lịch thi đã chuyển sang trạng thái Chờ duyệt."
                    : "Phân công thủ công thành công nhưng lịch thi vẫn thiếu giám thị.",
                ExamScheduleId = schedule.ExamScheduleId,
                AssignedCount = currentInvigilators.Count + plan.NewInvigilators.Count,
                StatusAfter = plan.StatusAfter
            };
        }

        private static ManualAssignmentResultDto Fail(int scheduleId, string message)
        {
            return new ManualAssignmentResultDto
            {
                Success = false,
                Message = message,
                ExamScheduleId = scheduleId,
                Errors = new List<string> { message }
            };
        }

        private static bool IsEditableStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals("Chờ phân công", StringComparison.OrdinalIgnoreCase)
                   || status.Equals("Thiếu giám thị", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildEditReason(string status, int currentCount, bool hasRejectedPosition)
        {
            if (hasRejectedPosition)
                return "Có giám thị đã từ chối. Thư ký có thể chọn người thay thế cho vị trí đó.";

            if (!IsEditableStatus(status))
                return $"Lịch đang ở trạng thái '{status}' nên không cho phép chỉnh sửa.";

            if (currentCount >= RequiredInvigilatorsPerSchedule)
                return "Lịch đã đủ 2 giám thị.";

            return string.Empty;
        }

        private static string BuildRecommendationLabel(bool isExactOwner, bool hasTaughtSubject, bool isLecturerRole, int currentLoad, int sameDayLoad, bool canSelect, ExamFormatPriority formatPriority)
        {
            if (!canSelect) return "Không phù hợp để chọn";
            if (isExactOwner) return formatPriority == ExamFormatPriority.Oral ? "Ưu tiên cao nhất: đang dạy lớp" : "Rất phù hợp: đang dạy lớp";
            if (hasTaughtSubject) return formatPriority == ExamFormatPriority.Standard ? "Phù hợp: có chuyên môn môn thi" : "Ưu tiên cao: có chuyên môn";
            if (!isLecturerRole) return "Dự phòng khi thiếu người";
            if (currentLoad == 0 && sameDayLoad == 0) return "Nên chọn: lịch đang nhẹ";
            if (sameDayLoad == 0) return "Phù hợp: chưa coi thi trong ngày";
            return "Có thể chọn nếu cần cân đối nhân sự";
        }

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
                "PTH" => "TH",
                _ => code
            };
        }

        private enum ExamFormatPriority
        {
            Standard = 0,
            Practical = 1,
            Oral = 2
        }

        private static string BuildWorkloadLabel(int currentLoad, int sameDayLoad)
        {
            var semesterText = currentLoad == 0 ? "chưa có lịch trong học kỳ" : $"đã có {currentLoad} lịch trong học kỳ";
            var dayText = sameDayLoad == 0 ? "rảnh trong ngày thi" : $"đã có {sameDayLoad} lịch trong ngày thi";
            return $"{semesterText}; {dayText}";
        }

        private static string BuildAvailabilityLabel(bool isBusy, bool isConflict, bool isAlreadyAssigned)
        {
            if (isAlreadyAssigned) return "Đã có trong lịch này";
            if (isBusy) return "Bận theo lịch cá nhân";
            if (isConflict) return "Trùng lịch coi thi khác";
            return "Có thể phân công";
        }
    }
}
