using ExamInvigilationManagement.Application.DTOs.Approval;
using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;

namespace ExamInvigilationManagement.Application.Services
{
    public class ExamScheduleApprovalService : IExamScheduleApprovalService
    {
        private const string RoleSecretary = "Thư ký khoa";
        private const string RoleDean = "Trưởng khoa";
        private const string StatusWaiting = "Chờ duyệt";
        private const string StatusApproved = "Đã duyệt";
        private const string StatusRejected = "Từ chối duyệt";
        private const string NotificationType = "ExamScheduleApproval";

        private readonly IExamScheduleApprovalRepository _repository;
        private readonly INotificationService _notificationService;

        public ExamScheduleApprovalService(
            IExamScheduleApprovalRepository repository,
            INotificationService notificationService)
        {
            _repository = repository;
            _notificationService = notificationService;
        }

        public async Task<ExamScheduleApprovalIndexPageDto> GetIndexAsync(
    ExamScheduleApprovalSearchDto search,
    int userId,
    int page = 1,
    int pageSize = 5,
    CancellationToken cancellationToken = default)
        {
            var context = await GetUserContextOrThrowAsync(userId, cancellationToken);

            if (!IsAllowedRole(context.RoleName))
                throw new InvalidOperationException("Người dùng không có quyền duyệt lịch thi.");

            if (!context.FacultyId.HasValue)
                throw new InvalidOperationException("Không xác định được khoa của người dùng.");

            var normalizedSearch = NormalizeSearch(search);

            var result = await _repository.GetIndexPageAsync(
                context.FacultyId.Value,
                normalizedSearch,
                page,
                pageSize,
                cancellationToken);

            return new ExamScheduleApprovalIndexPageDto
            {
                Search = normalizedSearch,
                PagedItems = result.PagedItems,
                TotalCount = result.TotalCount,
                ReviewableCount = result.ReviewableCount,
                NotEnoughCount = result.NotEnoughCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ExamScheduleApprovalBulkReviewResultDto> ReviewBulkAsync(
            ExamScheduleApprovalBulkReviewRequestDto request,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.SelectedExamScheduleIds is null || request.SelectedExamScheduleIds.Count == 0)
                errors.Add("Vui lòng chọn ít nhất một lịch thi.");

            var context = await _repository.GetUserContextAsync(userId, cancellationToken);
            if (context is null)
                errors.Add("Không xác định được người duyệt.");

            if (context is not null && !IsAllowedRole(context.RoleName))
                errors.Add("Người dùng không có quyền duyệt lịch thi.");

            if (errors.Count > 0)
            {
                return new ExamScheduleApprovalBulkReviewResultDto
                {
                    Success = false,
                    Message = "Không thể thực hiện duyệt hàng loạt.",
                    Errors = errors
                };
            }

            if (!context!.FacultyId.HasValue)
                return Fail("Không xác định được khoa của người duyệt.");

            var selectedIds = request.SelectedExamScheduleIds.Distinct().ToList();

            var targets = await _repository.GetBulkTargetsAsync(
                context.FacultyId.Value,
                selectedIds,
                cancellationToken);

            if (targets.Count != selectedIds.Count)
            {
                var foundIds = targets.Select(x => x.ExamScheduleId).ToHashSet();
                var missingIds = selectedIds.Where(id => !foundIds.Contains(id)).ToList();

                return new ExamScheduleApprovalBulkReviewResultDto
                {
                    Success = false,
                    Message = "Một số lịch thi không hợp lệ hoặc không thuộc khoa của bạn.",
                    Errors = missingIds.Select(id => $"Lịch thi #{id} không tồn tại hoặc không thuộc khoa của bạn.").ToList()
                };
            }

            var invalidTargets = targets
                .Where(x => !x.CanReview)
                .Select(x => $"Lịch thi #{x.ExamScheduleId}: {x.ReviewReason}")
                .ToList();

            if (invalidTargets.Count > 0)
            {
                return new ExamScheduleApprovalBulkReviewResultDto
                {
                    Success = false,
                    Message = "Không thể duyệt vì có lịch thi chưa hợp lệ.",
                    Errors = invalidTargets
                };
            }

            var finalStatus = request.IsApproved ? StatusApproved : StatusRejected;

            var plan = new ExamScheduleApprovalSavePlanDto
            {
                Items = targets.Select(x => new ExamScheduleApprovalSaveItemDto
                {
                    ExamScheduleId = x.ExamScheduleId,
                    ApproverId = userId,
                    Status = finalStatus,
                    Note = request.Note?.Trim(),
                    ApproveAt = DateTime.Now,
                    UpdateAt = DateTime.Now
                }).ToList()
            };

            await _repository.SaveBulkAsync(plan, cancellationToken);

            int notificationsSent = 0;

            if (context.RoleName.Equals(RoleDean, StringComparison.OrdinalIgnoreCase))
            {
                var secretaryIds = await _repository.GetSecretaryRecipientIdsAsync(
                    context.FacultyId.Value,
                    userId,
                    cancellationToken);

                var relatedScheduleId = targets.Count == 1 ? targets[0].ExamScheduleId : (int?)null;

                foreach (var secretaryId in secretaryIds)
                {
                    await _notificationService.CreateAsync(new NotificationWriteDto
                    {
                        UserId = secretaryId,
                        Title = request.IsApproved
                            ? $"Trưởng khoa đã duyệt {targets.Count} lịch thi"
                            : $"Trưởng khoa đã từ chối duyệt {targets.Count} lịch thi",
                        Content = BuildSummaryContent(targets, request.IsApproved, request.Note),
                        Type = NotificationType,
                        RelatedId = relatedScheduleId,
                        CreatedBy = userId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    }, cancellationToken);

                    notificationsSent++;
                }
            }

            return new ExamScheduleApprovalBulkReviewResultDto
            {
                Success = true,
                Message = request.IsApproved
                    ? "Đã duyệt hàng loạt lịch thi thành công."
                    : "Đã từ chối duyệt hàng loạt lịch thi thành công.",
                ProcessedCount = targets.Count,
                NotificationsSent = notificationsSent,
                StatusAfter = finalStatus
            };
        }

        private static ExamScheduleApprovalSearchDto NormalizeSearch(ExamScheduleApprovalSearchDto? search)
        {
            search ??= new ExamScheduleApprovalSearchDto();
            if (string.IsNullOrWhiteSpace(search.Status))
                search.Status = "Chờ duyệt";
            return search;
        }

        private async Task<ApprovalUserContextDto> GetUserContextOrThrowAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var context = await _repository.GetUserContextAsync(userId, cancellationToken);
            if (context is null)
                throw new InvalidOperationException("Không xác định được người dùng.");

            return context;
        }

        private static bool IsAllowedRole(string roleName)
        {
            return roleName.Equals(RoleSecretary, StringComparison.OrdinalIgnoreCase)
                   || roleName.Equals(RoleDean, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSummaryContent(
            IReadOnlyList<ExamScheduleApprovalIndexItemDto> targets,
            bool isApproved,
            string? note)
        {
            var actionText = isApproved ? "đã được duyệt" : "đã bị từ chối duyệt";
            var sampleRows = targets
                .Take(3)
                .Select(x =>
                    $"- {x.SubjectId} | {x.SubjectName} | {x.ClassName} | {x.RoomDisplay} | {x.ExamDate:dd/MM/yyyy} {x.TimeStart:HH\\:mm}")
                .ToList();

            var extraCount = Math.Max(0, targets.Count - sampleRows.Count);
            var extraText = extraCount > 0 ? $"\n... và {extraCount} lịch thi khác." : string.Empty;
            var noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : $"\nLý do: {note.Trim()}";

            return $"Có {targets.Count} lịch thi {actionText}.\n" +
                   string.Join("\n", sampleRows) +
                   extraText +
                   noteText;
        }

        private static ExamScheduleApprovalBulkReviewResultDto Fail(string message)
        {
            return new ExamScheduleApprovalBulkReviewResultDto
            {
                Success = false,
                Message = message,
                Errors = new List<string> { message }
            };
        }
    }
}
