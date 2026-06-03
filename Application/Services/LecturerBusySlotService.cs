using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Services
{
    public class LecturerBusySlotService : ILecturerBusySlotService
    {
        private readonly ILecturerBusySlotRepository _repo;

        public LecturerBusySlotService(ILecturerBusySlotRepository repo)
        {
            _repo = repo;
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

            var entity = new LecturerBusySlot
            {
                UserId = dto.UserId!.Value,
                SlotId = dto.ExamSlotId!.Value,
                BusyDate = dto.BusyDate,
                Note = dto.Note,
                CreateAt = dto.CreateAt ?? DateTime.Now
            };

            await _repo.AddAsync(entity);
        }

        public async Task<int> CreateManyAsync(LecturerBusySlotDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (!dto.UserId.HasValue) throw new InvalidOperationException("Thiếu giảng viên.");
            if (dto.BusyDate == default) throw new InvalidOperationException("Thiếu ngày bận.");

            var slotIds = dto.ExamSlotIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (slotIds.Count == 0 && dto.ExamSlotId.HasValue)
                slotIds.Add(dto.ExamSlotId.Value);

            if (slotIds.Count == 0)
                throw new InvalidOperationException("Vui lòng chọn ít nhất một ca bận.");

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
                    Note = dto.Note,
                    CreateAt = dto.CreateAt ?? DateTime.Now
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

            var exists = await _repo.ExistsAsync(
                dto.UserId!.Value,
                dto.ExamSlotId!.Value,
                dto.BusyDate,
                dto.Id);

            if (exists)
                throw new InvalidOperationException("Bạn đã đăng ký lịch bận cho ca này trong ngày này.");

            var entity = new LecturerBusySlot
            {
                Id = dto.Id,
                UserId = dto.UserId!.Value,
                SlotId = dto.ExamSlotId!.Value,
                BusyDate = dto.BusyDate,
                Note = dto.Note,
                CreateAt = dto.CreateAt
            };

            await _repo.UpdateAsync(entity);
        }

        public Task DeleteAsync(int id) => _repo.DeleteAsync(id);

        private static void Validate(LecturerBusySlotDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (!dto.UserId.HasValue) throw new InvalidOperationException("Thiếu giảng viên.");
            if (!dto.ExamSlotId.HasValue) throw new InvalidOperationException("Thiếu ca thi.");
            if (dto.BusyDate == default) throw new InvalidOperationException("Thiếu ngày bận.");
        }
    }
}
