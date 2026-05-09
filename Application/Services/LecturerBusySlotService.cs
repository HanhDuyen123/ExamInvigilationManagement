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