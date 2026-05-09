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

            dto.Status = ExamScheduleStatusHelper.Normalize(dto.Status);

            var offeringCtx = await _repo.GetOfferingContextAsync(dto.OfferingId!.Value);
            if (offeringCtx == null)
                throw new InvalidOperationException("Không tìm thấy học phần mở.");

            var slotCtx = await _repo.GetSlotContextAsync(dto.SlotId!.Value);
            if (slotCtx == null)
                throw new InvalidOperationException("Không tìm thấy ca thi.");

            if (!dto.RoomId.HasValue)
                throw new InvalidOperationException("Vui lòng chọn phòng thi.");

            if (!await _repo.RoomExistsAsync(dto.RoomId.Value))
                throw new InvalidOperationException("Phòng thi không tồn tại.");

            if (offeringCtx.AcademyYearId != slotCtx.AcademyYearId)
                throw new InvalidOperationException("Học phần mở và ca thi không thuộc cùng năm học.");

            if (offeringCtx.SemesterId != slotCtx.SemesterId)
                throw new InvalidOperationException("Học phần mở và ca thi không thuộc cùng học kỳ.");

            if (await _repo.ExistsRoomConflictAsync(dto.RoomId.Value, dto.ExamDate!.Value, dto.SlotId.Value, dto.Id))
                throw new InvalidOperationException("Phòng thi đã có lịch ở ca thi và ngày thi này.");

            var entity = new ExamSchedule
            {
                Id = dto.Id,
                OfferingId = dto.OfferingId.Value,
                SlotId = dto.SlotId.Value,
                RoomId = dto.RoomId.Value,
                ExamDate = dto.ExamDate.Value,
                Status = dto.Status!,

                AcademyYearId = slotCtx.AcademyYearId,
                SemesterId = slotCtx.SemesterId,
                PeriodId = slotCtx.PeriodId,
                SessionId = slotCtx.SessionId
            };

            await _repo.UpdateAsync(entity);
        }

        public Task DeleteAsync(int id) => _repo.DeleteAsync(id);

        public Task MarkApprovalRequestedAsync(IEnumerable<int> scheduleIds, IEnumerable<int> approverIds, string? note = null, CancellationToken cancellationToken = default)
            => _repo.MarkApprovalRequestedAsync(scheduleIds, approverIds, note, cancellationToken);

        private static void ValidateDto(ExamScheduleDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (!dto.OfferingId.HasValue) throw new InvalidOperationException("Vui lòng chọn học phần mở.");
            if (!dto.SlotId.HasValue) throw new InvalidOperationException("Vui lòng chọn ca thi.");
            if (!dto.ExamDate.HasValue) throw new InvalidOperationException("Vui lòng chọn ngày thi.");

            if (!ExamScheduleStatusHelper.IsValid(dto.Status))
                throw new InvalidOperationException("Trạng thái lịch thi không hợp lệ.");
        }
    }
}
