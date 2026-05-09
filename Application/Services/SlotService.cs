using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Repositories;

namespace ExamInvigilationManagement.Application.Services
{
    public class SlotService : ISlotService
    {
        private readonly ISlotRepository _repo;
        private readonly ISessionRepository _sessionRepo;

        public SlotService(ISlotRepository repo, ISessionRepository sessionRepo)
        {
            _repo = repo;
            _sessionRepo = sessionRepo;
        }
        public async Task<List<SlotDto>> GetAllBySessionAsync(int sessionId)
        {
            var slots = await _repo.GetAllBySessionAsync(sessionId);
            return slots.Select(s => new SlotDto
            {
                Id = s.Id,
                Name = s.Name,
                TimeStart = s.TimeStart
            }).ToList();
        }

        public async Task<SlotDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new SlotDto
            {
                Id = x.Id,
                Name = x.Name,
                TimeStart = x.TimeStart
            };
        }
        public async Task AddAsync(int sessionId, string name, TimeOnly timeStart)
        {
            name = (name ?? string.Empty).Trim();
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session == null)
                throw new InvalidOperationException("Buổi thi không hợp lệ.");

            var expectedSession = DefaultDataBuilder.Build().Semesters
                .SelectMany(x => x.Periods)
                .SelectMany(x => x.Sessions)
                .FirstOrDefault(x => SameName(x.Name, session.Name));

            if (expectedSession == null || !expectedSession.Slots.Any(x => SameName(x.Name, name) && x.TimeStart == timeStart))
                throw new InvalidOperationException("Ca thi không đúng cấu trúc chuẩn.");

            var current = await _repo.GetAllBySessionAsync(sessionId);
            if (current.Any(x => SameName(x.Name, name) && x.TimeStart == timeStart))
                throw new InvalidOperationException("Ca thi đã tồn tại.");

            var entity = new ExamSlot
            {
                Name = name,
                TimeStart = timeStart
            };

            await _repo.AddAsync(sessionId, entity);
        }
        public async Task UpdateAsync(SlotDto dto)
        {
            dto.Name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Vui lòng nhập tên ca thi.");

            var currentSlot = await _repo.GetByIdAsync(dto.Id);
            var session = currentSlot == null ? null : await _sessionRepo.GetByIdAsync(currentSlot.SessionId);
            var expectedSession = session == null
                ? null
                : DefaultDataBuilder.Build().Semesters
                    .SelectMany(x => x.Periods)
                    .SelectMany(x => x.Sessions)
                    .FirstOrDefault(x => SameName(x.Name, session.Name));

            if (expectedSession == null || !expectedSession.Slots.Any(x => SameName(x.Name, dto.Name) && x.TimeStart == dto.TimeStart))
                throw new InvalidOperationException("Ca thi không đúng cấu trúc chuẩn.");

            if (currentSlot != null)
            {
                var current = await _repo.GetAllBySessionAsync(currentSlot.SessionId);
                if (current.Any(x => x.Id != dto.Id && SameName(x.Name, dto.Name) && x.TimeStart == dto.TimeStart))
                    throw new InvalidOperationException("Ca thi đã tồn tại.");
            }

            await _repo.UpdateAsync(dto.Id, dto.Name, dto.TimeStart);
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }

        //public async Task UpdateAsync(SlotDto dto)
        //{
        //    var slot = await _repo.GetByIdAsync(dto.Id);
        //    if (slot == null) return;

        //    slot.Name = dto.Name;
        //    slot.TimeStart = dto.TimeStart;

        //    await _repo.UpdateAsync(slot);
        //}

        //public async Task DeleteAsync(int id)
        //{
        //    await _repo.DeleteAsync(id);
        //}

        private static bool SameName(string? current, string? expected)
        {
            return string.Equals(current?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
