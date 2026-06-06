using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Constants;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Services
{
    public class LecturerBusySlotService : ILecturerBusySlotService
    {
        private readonly ILecturerBusySlotRepository _repo;
        private readonly INotificationService _notificationService;

        public LecturerBusySlotService(
            ILecturerBusySlotRepository repo,
            INotificationService notificationService)
        {
            _repo = repo;
            _notificationService = notificationService;
        }

        public Task<PagedResult<LecturerBusySlotDto>> GetPagedAsync(LecturerBusySlotSearchDto filter, int page, int pageSize)
            => _repo.GetPagedAsync(filter, page, pageSize);

        public Task<LecturerBusySlotDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task CreateAsync(LecturerBusySlotDto dto)
        {
            Validate(dto);

            var exists = await _repo.ExistsAsync(
                dto.UserId!.Value,
                dto.ExamSlotId!.Value,
                dto.BusyDate);

            if (exists)
                throw new InvalidOperationException("Bạn đã đăng ký lịch bận cho ca này trong ngày này.");

            await EnsureAssignmentNotStartedForSlotAsync(dto.UserId.Value, dto.ExamSlotId.Value);

            var entity = new LecturerBusySlot
            {
                UserId = dto.UserId!.Value,
                SlotId = dto.ExamSlotId!.Value,
                BusyDate = dto.BusyDate,
                Note = dto.Note!.Trim(),
                CreateAt = dto.CreateAt ?? DateTime.Now,
                ApprovalStatus = BusyApprovalStatuses.Pending
            };

            await _repo.AddAsync(entity);
        }

        public async Task<int> CreateManyAsync(LecturerBusySlotDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (!dto.UserId.HasValue) throw new InvalidOperationException("Thiếu giảng viên.");
            if (string.IsNullOrWhiteSpace(dto.Note)) throw new InvalidOperationException("Vui lòng nhập lý do đăng ký lịch bận.");

            if (dto.BusyWholePeriod)
            {
                if (!dto.ExamPeriodId.HasValue) throw new InvalidOperationException("Thiếu đợt thi.");
                if (await _repo.IsPeriodInExpiredSemesterAsync(dto.ExamPeriodId.Value, DateTime.Today))
                    throw new InvalidOperationException("Không thể đăng ký lịch bận cho học kỳ đã kết thúc.");
                if (await _repo.BusyPeriodExistsAsync(dto.UserId.Value, dto.ExamPeriodId.Value))
                    throw new InvalidOperationException("Bạn đã đăng ký bận cả đợt thi này.");
                await EnsureAssignmentNotStartedForPeriodAsync(dto.UserId.Value, dto.ExamPeriodId.Value);

                await _repo.AddBusyPeriodAsync(dto.UserId.Value, dto.ExamPeriodId.Value, dto.Note.Trim());
                return 1;
            }

            if (dto.BusyDate == default) throw new InvalidOperationException("Thiếu ngày bận.");

            var slotIds = dto.ExamSlotIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (slotIds.Count == 0 && dto.ExamSlotId.HasValue)
                slotIds.Add(dto.ExamSlotId.Value);

            if (slotIds.Count == 0)
                throw new InvalidOperationException("Vui lòng chọn ít nhất một ca bận.");

            if (await _repo.AnySlotInExpiredSemesterAsync(slotIds, DateTime.Today))
                throw new InvalidOperationException("Không thể đăng ký lịch bận cho học kỳ đã kết thúc.");

            foreach (var slotId in slotIds)
                await EnsureAssignmentNotStartedForSlotAsync(dto.UserId.Value, slotId);

            var entities = new List<LecturerBusySlot>();
            foreach (var slotId in slotIds)
            {
                var exists = await _repo.ExistsAsync(dto.UserId.Value, slotId, dto.BusyDate);
                if (exists) continue;

                entities.Add(new LecturerBusySlot
                {
                    UserId = dto.UserId.Value,
                    SlotId = slotId,
                    BusyDate = dto.BusyDate,
                    Note = dto.Note.Trim(),
                    CreateAt = dto.CreateAt ?? DateTime.Now,
                    ApprovalStatus = BusyApprovalStatuses.Pending
                });
            }

            if (entities.Count == 0)
                throw new InvalidOperationException("Các ca đã chọn đều đã được đăng ký lịch bận.");

            await _repo.AddRangeAsync(entities);
            return entities.Count;
        }

        public async Task UpdateAsync(LecturerBusySlotDto dto)
        {
            Validate(dto);

            var current = await _repo.GetByIdAsync(dto.Id);
            if (current == null) throw new InvalidOperationException("Không tìm thấy lịch bận.");
            if (BusyApprovalStatuses.IsFinal(current.ApprovalStatus))
                throw new InvalidOperationException("Không thể cập nhật lịch bận đã được duyệt hoặc từ chối.");

            var exists = await _repo.ExistsAsync(
                dto.UserId!.Value,
                dto.ExamSlotId!.Value,
                dto.BusyDate,
                dto.Id);

            if (exists)
                throw new InvalidOperationException("Bạn đã đăng ký lịch bận cho ca này trong ngày này.");

            await EnsureAssignmentNotStartedForSlotAsync(dto.UserId.Value, dto.ExamSlotId.Value);

            var entity = new LecturerBusySlot
            {
                Id = dto.Id,
                UserId = dto.UserId!.Value,
                SlotId = dto.ExamSlotId!.Value,
                BusyDate = dto.BusyDate,
                Note = dto.Note!.Trim(),
                CreateAt = dto.CreateAt,
                ApprovalStatus = BusyApprovalStatuses.Pending
            };

            await _repo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current == null) return;
            if (BusyApprovalStatuses.IsFinal(current.ApprovalStatus))
                throw new InvalidOperationException("Không thể xoá lịch bận đã được duyệt hoặc từ chối.");
            await _repo.DeleteAsync(id);
        }

        public Task ApproveAsync(int id, int approverId) => _repo.ApproveAsync(id, approverId);

        public Task RejectAsync(int id, int approverId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Vui lòng nhập lý do từ chối.");
            return _repo.RejectAsync(id, approverId, reason.Trim());
        }

        public async Task NotifyBusyRegistrationAsync(LecturerBusySlotDto dto, int createdCount, CancellationToken cancellationToken = default)
        {
            if (!dto.UserId.HasValue || createdCount <= 0) return;

            var deanIds = await _repo.GetDeanIdsForLecturerAsync(dto.UserId.Value, cancellationToken);
            if (deanIds.Count == 0) return;

            var lecturerName = await _repo.GetLecturerDisplayNameAsync(dto.UserId.Value, cancellationToken);
            var scope = dto.BusyWholePeriod ? "cả đợt thi" : $"{createdCount} ca thi";
            var title = "Giảng viên đăng ký lịch bận";
            var content = $"{lecturerName} vừa đăng ký bận {scope}. Vui lòng kiểm tra và duyệt trên trang Lịch bận.";

            foreach (var deanId in deanIds)
            {
                await _notificationService.CreateAsync(new NotificationWriteDto
                {
                    UserId = deanId,
                    Title = title,
                    Content = content,
                    Type = NotificationTypes.LecturerBusyRegistration,
                    CreatedBy = dto.UserId.Value,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                }, cancellationToken);
            }
        }

        public Task<PagedResult<LecturerPeriodAvailabilityDto>> GetAvailabilityPagedAsync(LecturerPeriodAvailabilitySearchDto filter, int page, int pageSize)
            => _repo.GetAvailabilityPagedAsync(filter, page, pageSize);

        private static void Validate(LecturerBusySlotDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (!dto.UserId.HasValue) throw new InvalidOperationException("Thiếu giảng viên.");
            if (!dto.ExamSlotId.HasValue) throw new InvalidOperationException("Thiếu ca thi.");
            if (dto.BusyDate == default) throw new InvalidOperationException("Thiếu ngày bận.");
            if (string.IsNullOrWhiteSpace(dto.Note)) throw new InvalidOperationException("Vui lòng nhập lý do đăng ký lịch bận.");
        }

        private async Task EnsureAssignmentNotStartedForSlotAsync(int lecturerUserId, int slotId)
        {
            if (await _repo.HasEffectiveAssignmentForLecturerSlotAsync(lecturerUserId, slotId))
                throw new InvalidOperationException("Đợt thi của khoa đã bắt đầu phân công giám thị, không thể đăng ký hoặc cập nhật lịch bận.");
        }

        private async Task EnsureAssignmentNotStartedForPeriodAsync(int lecturerUserId, int periodId)
        {
            if (await _repo.HasEffectiveAssignmentForLecturerPeriodAsync(lecturerUserId, periodId))
                throw new InvalidOperationException("Đợt thi của khoa đã bắt đầu phân công giám thị, không thể đăng ký hoặc cập nhật lịch bận.");
        }
    }
}
