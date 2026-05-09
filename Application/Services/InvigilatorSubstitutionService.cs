using ExamInvigilationManagement.Application.DTOs.InvigilatorSubstitution;
using ExamInvigilationManagement.Application.DTOs.ManualAssignment;
using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;

namespace ExamInvigilationManagement.Application.Services
{
    public class InvigilatorSubstitutionService : IInvigilatorSubstitutionService
    {
        private readonly IInvigilatorSubstitutionRepository _repository;
        private readonly INotificationService _notificationService;

        public InvigilatorSubstitutionService(IInvigilatorSubstitutionRepository repository, INotificationService notificationService)
        {
            _repository = repository;
            _notificationService = notificationService;
        }

        public async Task<InvigilatorSubstitutionCreatePageDto> GetCreatePageAsync(int examInvigilatorId, int userId, CancellationToken cancellationToken = default)
        {
            var assignment = await _repository.GetRejectedAssignmentAsync(examInvigilatorId, userId, cancellationToken);
            if (assignment == null)
                throw new InvalidOperationException("Không tìm thấy lịch coi thi bị từ chối thuộc tài khoản của bạn.");

            if (!string.Equals(assignment.ResponseStatus, "Từ chối", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể đề xuất thay thế cho lịch coi thi đã từ chối.");

            if (await _repository.HasAnyAsync(examInvigilatorId, userId, cancellationToken))
                throw new InvalidOperationException("Lịch thi này đã được đề xuất thay thế. Mỗi lịch coi thi chỉ được đề xuất thay thế một lần.");

            var options = await BuildLecturerOptionsAsync(assignment, assignment.CurrentAssigneeId, cancellationToken);
            return new InvigilatorSubstitutionCreatePageDto
            {
                Schedule = assignment,
                LecturerOptions = options,
                Request = new InvigilatorSubstitutionCreateRequestDto { ExamInvigilatorId = examInvigilatorId }
            };
        }

        public async Task<InvigilatorSubstitutionResultDto> CreateAsync(InvigilatorSubstitutionCreateRequestDto request, int userId, CancellationToken cancellationToken = default)
        {
            if (request.ExamInvigilatorId <= 0 || request.SubstituteUserId <= 0)
                return Fail("Vui lòng chọn giảng viên thay thế hợp lệ.");

            var page = await GetCreatePageAsync(request.ExamInvigilatorId, userId, cancellationToken);
            if (await _repository.HasAnyAsync(request.ExamInvigilatorId, userId, cancellationToken))
                return Fail("Lịch thi này đã được đề xuất thay thế. Mỗi lịch coi thi chỉ được đề xuất thay thế một lần.");

            var substitute = page.LecturerOptions.FirstOrDefault(x => x.UserId == request.SubstituteUserId);
            if (substitute == null || !substitute.CanSelect)
                return Fail("Giảng viên thay thế chưa đáp ứng điều kiện phân công cho lịch thi này.");

            var substitutionId = await _repository.CreateAsync(request.ExamInvigilatorId, userId, request.SubstituteUserId, cancellationToken);
            await NotifySecretariesAsync(page.Schedule, userId, substitute, substitutionId, cancellationToken);

            return new InvigilatorSubstitutionResultDto
            {
                Success = true,
                Message = "Đã gửi đề xuất thay thế đến Thư ký khoa.",
                RelatedId = substitutionId
            };
        }

        public async Task<InvigilatorSubstitutionIndexPageDto> GetIndexAsync(int secretaryId, InvigilatorSubstitutionSearchDto search, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var facultyId = await GetSecretaryFacultyIdAsync(secretaryId, cancellationToken);
            return new InvigilatorSubstitutionIndexPageDto
            {
                Search = search,
                PagedItems = await _repository.GetPagedAsync(facultyId, search, page, pageSize, cancellationToken)
            };
        }

        public async Task<InvigilatorSubstitutionDetailDto> GetDetailAsync(int substitutionId, int secretaryId, CancellationToken cancellationToken = default)
        {
            var facultyId = await GetSecretaryFacultyIdAsync(secretaryId, cancellationToken);
            var detail = await _repository.GetDetailAsync(substitutionId, facultyId, cancellationToken);
            if (detail == null) throw new InvalidOperationException("Không tìm thấy đề xuất thay thế thuộc khoa của bạn.");

            var assignment = await _repository.GetAssignmentForReviewAsync(detail.ExamInvigilatorId, cancellationToken);
            if (assignment != null)
            {
                var options = await BuildLecturerOptionsAsync(assignment, assignment.CurrentAssigneeId, cancellationToken);
                detail.ReplacementOptions = options;
                detail.SubstituteEvaluation = options.FirstOrDefault(x => x.UserId == detail.SubstituteUserId);
                detail.CanApprove = detail.Status == "Đã đề xuất" && detail.SubstituteEvaluation?.CanSelect == true;
                detail.ApproveReason = detail.CanApprove ? "Đủ điều kiện thay thế." : detail.SubstituteEvaluation?.Reason ?? "Không thể đánh giá điều kiện thay thế.";
            }

            return detail;
        }

        public async Task<InvigilatorSubstitutionResultDto> ApproveAsync(int substitutionId, int secretaryId, CancellationToken cancellationToken = default)
        {
            var detail = await GetDetailAsync(substitutionId, secretaryId, cancellationToken);
            if (!detail.CanApprove)
                return Fail($"Chưa thể duyệt đề xuất này. {detail.ApproveReason}");

            await _repository.ApproveAsync(substitutionId, secretaryId, cancellationToken);
            return new InvigilatorSubstitutionResultDto { Success = true, Message = "Đã duyệt đề xuất và cập nhật giảng viên thay thế vào lịch phân công.", RelatedId = substitutionId };
        }

        public async Task<InvigilatorSubstitutionResultDto> ApproveWithReplacementAsync(int substitutionId, int replacementUserId, int secretaryId, CancellationToken cancellationToken = default)
        {
            var detail = await GetDetailAsync(substitutionId, secretaryId, cancellationToken);
            if (detail.Status != "Đã đề xuất") return Fail("Đề xuất này đã được xử lý.");
            if (replacementUserId <= 0) return Fail("Vui lòng chọn giảng viên thay thế.");
            if (replacementUserId == detail.SubstituteUserId)
                return await ApproveAsync(substitutionId, secretaryId, cancellationToken);

            var assignment = await _repository.GetAssignmentForReviewAsync(detail.ExamInvigilatorId, cancellationToken);
            if (assignment == null) return Fail("Không tìm thấy lịch phân công cần thay thế.");
            var options = await BuildLecturerOptionsAsync(assignment, assignment.CurrentAssigneeId, cancellationToken);
            var replacement = options.FirstOrDefault(x => x.UserId == replacementUserId);
            if (replacement == null || !replacement.CanSelect)
                return Fail("Giảng viên được chọn chưa đáp ứng điều kiện thay thế cho lịch thi này.");

            await _repository.ApproveWithReplacementAsync(substitutionId, replacementUserId, secretaryId, cancellationToken);
            return new InvigilatorSubstitutionResultDto { Success = true, Message = "Đã chọn giảng viên thay thế khác và đưa lịch thi về trạng thái Chờ duyệt.", RelatedId = substitutionId };
        }

        public async Task<InvigilatorSubstitutionResultDto> RejectAsync(int substitutionId, int secretaryId, CancellationToken cancellationToken = default)
        {
            _ = await GetDetailAsync(substitutionId, secretaryId, cancellationToken);
            await _repository.RejectAsync(substitutionId, cancellationToken);
            return new InvigilatorSubstitutionResultDto { Success = true, Message = "Đã từ chối đề xuất thay thế.", RelatedId = substitutionId };
        }

        private async Task<List<ManualAssignmentLecturerOptionDto>> BuildLecturerOptionsAsync(InvigilatorSubstitutionScheduleDto schedule, int rejectedUserId, CancellationToken cancellationToken)
        {
            var lecturers = await _repository.GetActiveLecturersAsync(schedule.FacultyId, cancellationToken);
            var lecturerIds = lecturers.Select(x => x.UserId).ToList();
            var busyIds = await _repository.GetBusyLecturerIdsAsync(lecturerIds, schedule.SlotId, DateOnly.FromDateTime(schedule.ExamDate), cancellationToken);
            var conflictIds = await _repository.GetConflictingLecturerIdsAsync(schedule.ExamScheduleId, schedule.SemesterId, schedule.PeriodId, schedule.SessionId, schedule.SlotId, lecturerIds, cancellationToken);
            var loads = await _repository.GetLecturerLoadsAsync(schedule.SemesterId, schedule.FacultyId, cancellationToken);
            var periodLoads = await _repository.GetPeriodLoadsAsync(schedule.SemesterId, schedule.PeriodId, schedule.FacultyId, cancellationToken);
            var sameDayLoads = await _repository.GetSameDayLoadsAsync(schedule.SemesterId, schedule.FacultyId, schedule.ExamDate, cancellationToken);
            var subjectTeacherIds = await _repository.GetSubjectTeacherIdsAsync(schedule.SemesterId, schedule.SubjectId, schedule.FacultyId, cancellationToken);
            var classTeacherIds = await _repository.GetClassTeacherIdsAsync(schedule.SemesterId, schedule.SubjectId, schedule.ClassName, schedule.GroupNumber, schedule.FacultyId, cancellationToken);

            var avgLoad = loads.Count == 0 ? 0 : loads.Values.Average();
            var avgPeriodLoad = periodLoads.Count == 0 ? 0 : periodLoads.Values.Average();
            var avgDayLoad = sameDayLoads.Count == 0 ? 0 : sameDayLoads.Values.Average();

            return lecturers.Select(x =>
            {
                var isBusy = busyIds.Contains(x.UserId);
                var isConflict = conflictIds.Contains(x.UserId);
                var isRejectedUser = x.UserId == rejectedUserId;
                var currentLoad = loads.TryGetValue(x.UserId, out var load) ? load : 0;
                var periodLoad = periodLoads.TryGetValue(x.UserId, out var pLoad) ? pLoad : 0;
                var sameDayLoad = sameDayLoads.TryGetValue(x.UserId, out var dayLoad) ? dayLoad : 0;
                var hasTaughtSubject = subjectTeacherIds.Contains(x.UserId);
                var hasTaughtClass = classTeacherIds.Contains(x.UserId);
                var isExactOwner = x.UserId == schedule.OfferingUserId;
                var priorityScore = 1000;
                priorityScore -= currentLoad * 20;
                priorityScore -= periodLoad * 28;
                priorityScore -= sameDayLoad * 45;
                if (currentLoad <= avgLoad) priorityScore += 45;
                if (periodLoad <= avgPeriodLoad) priorityScore += 55;
                if (sameDayLoad <= avgDayLoad) priorityScore += 80;
                if (hasTaughtSubject) priorityScore += 90;
                if (hasTaughtClass) priorityScore += 130;
                if (isExactOwner) priorityScore += 160;

                var reasons = new List<string>
                {
                    currentLoad == 0 ? "Chưa có lịch coi thi nào trong học kỳ" : $"Đã có {currentLoad} lịch coi thi trong học kỳ",
                    periodLoad == 0 ? "Chưa có lịch trong đợt thi này" : $"Đã có {periodLoad} lịch trong đợt thi này",
                    sameDayLoad == 0 ? "Chưa có lịch coi thi trong ngày này" : $"Đã có {sameDayLoad} lịch coi thi trong cùng ngày"
                };
                if (hasTaughtSubject) reasons.Add("Có kinh nghiệm với môn thi này");
                if (hasTaughtClass) reasons.Add("Đang/đã phụ trách đúng lớp học phần");
                if (isRejectedUser) reasons.Add("Là giảng viên đã từ chối lịch này");
                if (isBusy) reasons.Add("Giảng viên đã đăng ký bận vào ca này");
                if (isConflict) reasons.Add("Đã có lịch coi thi khác trùng đợt, buổi và ca");

                x.CurrentLoad = currentLoad;
                x.PeriodLoad = periodLoad;
                x.SameDayLoad = sameDayLoad;
                x.HasTaughtSubject = hasTaughtSubject;
                x.HasTaughtClass = hasTaughtClass;
                x.IsExactOwner = isExactOwner;
                x.PriorityScore = priorityScore;
                x.CanSelect = !isRejectedUser && !isBusy && !isConflict;
                x.Reason = string.Join("; ", reasons);
                x.RecommendationLabel = BuildRecommendationLabel(isExactOwner, hasTaughtClass, hasTaughtSubject, currentLoad, periodLoad, sameDayLoad, x.CanSelect);
                x.WorkloadLabel = BuildWorkloadLabel(currentLoad, periodLoad, sameDayLoad);
                x.AvailabilityLabel = BuildAvailabilityLabel(isRejectedUser, isBusy, isConflict);
                return x;
            })
            .OrderByDescending(x => x.CanSelect)
            .ThenByDescending(x => x.PriorityScore)
            .ThenByDescending(x => x.HasTaughtClass)
            .ThenByDescending(x => x.HasTaughtSubject)
            .ThenBy(x => x.CurrentLoad)
            .ThenBy(x => x.PeriodLoad)
            .ThenBy(x => x.SameDayLoad)
            .ThenBy(x => x.FullName)
            .ToList();
        }

        private async Task NotifySecretariesAsync(InvigilatorSubstitutionScheduleDto schedule, int lecturerId, ManualAssignmentLecturerOptionDto substitute, int substitutionId, CancellationToken cancellationToken)
        {
            var secretaries = await _repository.GetActiveSecretariesAsync(schedule.FacultyId, cancellationToken);
            var title = "Giảng viên đề xuất người thay thế";
            var content = $"Giảng viên đã từ chối lịch {schedule.SubjectId} - {schedule.ClassName} - Nhóm {schedule.GroupNumber} ngày {schedule.ExamDate:dd/MM/yyyy}, ca {schedule.SlotName}. " +
                          $"Người được đề xuất thay thế: {substitute.FullName} ({substitute.UserName}).";

            foreach (var secretary in secretaries)
            {
                await _notificationService.CreateAsync(new NotificationWriteDto
                {
                    UserId = secretary.UserId,
                    Title = title,
                    Content = content,
                    Type = NotificationTypes.InvigilatorSubstitution,
                    RelatedId = substitutionId,
                    CreatedBy = lecturerId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }, cancellationToken);
            }
        }

        private async Task<int> GetSecretaryFacultyIdAsync(int secretaryId, CancellationToken cancellationToken)
        {
            var facultyId = await _repository.GetUserFacultyIdAsync(secretaryId, cancellationToken);
            if (!facultyId.HasValue) throw new InvalidOperationException("Không xác định được khoa của tài khoản Thư ký khoa.");
            return facultyId.Value;
        }

        private static string BuildRecommendationLabel(bool isExactOwner, bool hasTaughtClass, bool hasTaughtSubject, int currentLoad, int periodLoad, int sameDayLoad, bool canSelect)
        {
            if (!canSelect) return "Không phù hợp để chọn";
            if (isExactOwner || hasTaughtClass) return "Rất phù hợp: hiểu rõ lớp thi";
            if (hasTaughtSubject) return "Phù hợp: có kinh nghiệm với môn thi";
            if (currentLoad == 0 && periodLoad == 0 && sameDayLoad == 0) return "Nên chọn: lịch đang nhẹ";
            if (sameDayLoad == 0) return "Có thể chọn: chưa coi thi trong ngày";
            return "Có thể chọn nếu cần cân đối nhân sự";
        }

        private static string BuildWorkloadLabel(int currentLoad, int periodLoad, int sameDayLoad)
        {
            var semesterText = currentLoad == 0 ? "chưa có lịch trong học kỳ" : $"đã có {currentLoad} lịch trong học kỳ";
            var periodText = periodLoad == 0 ? "chưa có lịch trong đợt thi" : $"đã có {periodLoad} lịch trong đợt thi";
            var dayText = sameDayLoad == 0 ? "rảnh trong ngày thi" : $"đã có {sameDayLoad} lịch trong ngày thi";
            return $"{semesterText}; {periodText}; {dayText}";
        }

        private static string BuildAvailabilityLabel(bool isRejectedUser, bool isBusy, bool isConflict)
        {
            if (isRejectedUser) return "Người đã từ chối lịch này";
            if (isBusy) return "Bận theo lịch cá nhân";
            if (isConflict) return "Trùng lịch coi thi khác";
            return "Có thể thay thế";
        }

        private static InvigilatorSubstitutionResultDto Fail(string message)
        {
            return new InvigilatorSubstitutionResultDto { Success = false, Message = message, Errors = new List<string> { message } };
        }
    }
}
