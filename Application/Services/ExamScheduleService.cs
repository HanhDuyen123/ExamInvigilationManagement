using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Services
{
    public class ExamScheduleService : IExamScheduleService
    {
        private readonly IExamScheduleRepository _repo;

        public ExamScheduleService(IExamScheduleRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<ExamScheduleDto>> GetPagedAsync(ExamScheduleSearchDto filter, int page, int pageSize)
            => _repo.GetPagedAsync(filter, page, pageSize);

        public Task<ExamScheduleDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task CreateAsync(ExamScheduleDto dto)
        {
            ValidateDto(dto);

            dto.Status = ExamScheduleStatusHelper.Normalize(dto.Status);

            var offeringCtx = await _repo.GetOfferingContextAsync(dto.OfferingId!.Value);
            if (offeringCtx == null)
                throw new InvalidOperationException("Không tìm thấy học phần mở.");

            var slotCtx = await _repo.GetSlotContextAsync(dto.SlotId!.Value);
            if (slotCtx == null)
                throw new InvalidOperationException("Không tìm thấy ca thi.");

            if (!await _repo.ExamFormatExistsAsync(dto.ExamFormatId!.Value))
                throw new InvalidOperationException("Hình thức thi không tồn tại hoặc đã ngừng sử dụng.");

            var roomIds = dto.RoomIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList()
                ?? new List<int>();

            if (!roomIds.Any())
                throw new InvalidOperationException("Vui lòng chọn ít nhất 1 phòng thi.");

            if (offeringCtx.AcademyYearId != slotCtx.AcademyYearId)
                throw new InvalidOperationException("Học phần mở và ca thi không thuộc cùng năm học.");

            if (offeringCtx.SemesterId != slotCtx.SemesterId)
                throw new InvalidOperationException("Học phần mở và ca thi không thuộc cùng học kỳ.");

            ValidateExamDateInOfferingSemester(dto.ExamDate!.Value, offeringCtx);

            foreach (var roomId in roomIds)
            {
                if (!await _repo.RoomExistsAsync(roomId))
                    throw new InvalidOperationException($"Phòng thi {roomId} không tồn tại.");

                if (await _repo.ExistsRoomConflictAsync(roomId, dto.ExamDate!.Value, dto.SlotId.Value))
                    throw new InvalidOperationException("Phòng thi đã có lịch ở ca thi và ngày thi này.");
            }

            foreach (var roomId in roomIds)
            {
                var entity = new ExamSchedule
                {
                    OfferingId = dto.OfferingId.Value,
                    ExamFormatId = dto.ExamFormatId.Value,
                    SlotId = dto.SlotId.Value,
                    RoomId = roomId,
                    ExamDate = dto.ExamDate.Value,
                    Status = dto.Status!,

                    AcademyYearId = slotCtx.AcademyYearId,
                    SemesterId = slotCtx.SemesterId,
                    PeriodId = slotCtx.PeriodId,
                    SessionId = slotCtx.SessionId
                };

                await _repo.AddAsync(entity);
            }
        }

        public async Task UpdateAsync(ExamScheduleDto dto)
        {
            ValidateDto(dto);

            if (await _repo.HasInvigilatorsInRoomGroupAsync(dto.Id))
                throw new InvalidOperationException("Nhóm lịch thi này đã được phân công giám thị, không thể sửa để tránh lệch dữ liệu phân công.");

            dto.Status = ExamScheduleStatusHelper.Normalize(dto.Status);

            var offeringCtx = await _repo.GetOfferingContextAsync(dto.OfferingId!.Value);
            if (offeringCtx == null)
                throw new InvalidOperationException("Không tìm thấy học phần mở.");

            var slotCtx = await _repo.GetSlotContextAsync(dto.SlotId!.Value);
            if (slotCtx == null)
                throw new InvalidOperationException("Không tìm thấy ca thi.");

            if (!await _repo.ExamFormatExistsAsync(dto.ExamFormatId!.Value))
                throw new InvalidOperationException("Hình thức thi không tồn tại hoặc đã ngừng sử dụng.");

            var roomIds = dto.RoomIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList()
                ?? new List<int>();

            if (roomIds.Count == 0 && dto.RoomId.HasValue)
                roomIds.Add(dto.RoomId.Value);

            if (roomIds.Count == 0)
                throw new InvalidOperationException("Vui lòng chọn ít nhất 1 phòng thi.");

            if (offeringCtx.AcademyYearId != slotCtx.AcademyYearId)
                throw new InvalidOperationException("Học phần mở và ca thi không thuộc cùng năm học.");

            if (offeringCtx.SemesterId != slotCtx.SemesterId)
                throw new InvalidOperationException("Học phần mở và ca thi không thuộc cùng học kỳ.");

            ValidateExamDateInOfferingSemester(dto.ExamDate!.Value, offeringCtx);

            var ignoreIds = await _repo.GetScheduleIdsInRoomGroupAsync(dto.Id);
            foreach (var roomId in roomIds)
            {
                if (!await _repo.RoomExistsAsync(roomId))
                    throw new InvalidOperationException($"Phòng thi {roomId} không tồn tại.");

                if (await _repo.ExistsRoomConflictAsync(roomId, dto.ExamDate!.Value, dto.SlotId.Value, ignoreIds))
                    throw new InvalidOperationException("Phòng thi đã có lịch ở ca thi và ngày thi này.");
            }

            var entity = new ExamSchedule
            {
                Id = dto.Id,
                OfferingId = dto.OfferingId.Value,
                ExamFormatId = dto.ExamFormatId.Value,
                SlotId = dto.SlotId.Value,
                RoomId = roomIds.First(),
                ExamDate = dto.ExamDate.Value,
                Status = dto.Status!,

                AcademyYearId = slotCtx.AcademyYearId,
                SemesterId = slotCtx.SemesterId,
                PeriodId = slotCtx.PeriodId,
                SessionId = slotCtx.SessionId
            };

            await _repo.UpdateRoomGroupAsync(dto.Id, entity, roomIds);
        }

        public async Task DeleteAsync(int id)
        {
            if (await _repo.HasInvigilatorsAsync(id))
                throw new InvalidOperationException("Lịch thi đã được phân công giám thị, không thể xóa để tránh mất dữ liệu nghiệp vụ liên quan.");

            await _repo.DeleteAsync(id);
        }

        public Task<List<ExamFormatDto>> GetExamFormatsAsync(CancellationToken cancellationToken = default)
            => _repo.GetExamFormatsAsync(cancellationToken);

        public Task MarkApprovalRequestedAsync(IEnumerable<int> scheduleIds, IEnumerable<int> approverIds, int? requestedById = null, int? facultyId = null, string? note = null, CancellationToken cancellationToken = default)
            => _repo.MarkApprovalRequestedAsync(scheduleIds, approverIds, requestedById, facultyId, note, cancellationToken);

        public Task MarkSupportRequestedAsync(IEnumerable<int> scheduleIds, int requestedById, CancellationToken cancellationToken = default)
            => _repo.MarkSupportRequestedAsync(scheduleIds, requestedById, cancellationToken);

        private static void ValidateDto(ExamScheduleDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (!dto.OfferingId.HasValue) throw new InvalidOperationException("Vui lòng chọn học phần mở.");
            if (!dto.SlotId.HasValue) throw new InvalidOperationException("Vui lòng chọn ca thi.");
            if (!dto.ExamFormatId.HasValue) throw new InvalidOperationException("Vui lòng chọn hình thức thi.");
            if (!dto.ExamDate.HasValue) throw new InvalidOperationException("Vui lòng chọn ngày thi.");

            if (!ExamScheduleStatusHelper.IsValid(dto.Status))
                throw new InvalidOperationException("Trạng thái lịch thi không hợp lệ.");
        }

        private static void ValidateExamDateInOfferingSemester(DateTime examDate, ExamScheduleValidationContextDto offeringCtx)
        {
            var date = examDate.Date;
            if (offeringCtx.SemesterStartDate.HasValue && date < offeringCtx.SemesterStartDate.Value.Date)
                throw new InvalidOperationException("Ngày thi phải nằm trong thời gian học kỳ của học phần mở.");

            if (offeringCtx.SemesterEndDate.HasValue && date > offeringCtx.SemesterEndDate.Value.Date)
                throw new InvalidOperationException("Ngày thi phải nằm trong thời gian học kỳ của học phần mở.");
        }
    }
}
