using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Repositories;

namespace ExamInvigilationManagement.Application.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _repo;
        private readonly IPeriodRepository _periodRepo;

        public SessionService(ISessionRepository repo, IPeriodRepository periodRepo)
        {
            _repo = repo;
            _periodRepo = periodRepo;
        }
        public async Task<List<SessionDto>> GetAllByPeriodAsync(int periodId)
        {
            var sessions = await _repo.GetAllByPeriodAsync(periodId);
            return sessions.Select(s => new SessionDto
            {
                Id = s.Id,
                Name = s.Name
            }).ToList();
        }
        public async Task<string> GetSessionNameAsync(int sessionId)
        {
            var session = await _repo.GetByIdAsync(sessionId);
            return session?.Name ?? string.Empty;
        }
        public async Task AddAsync(int periodId, string name)
        {
            name = (name ?? string.Empty).Trim();
            var period = await _periodRepo.GetByIdAsync(periodId);
            if (period == null)
                throw new InvalidOperationException("Đợt thi không hợp lệ.");

            var expected = DefaultDataBuilder.Build().Semesters
                .SelectMany(x => x.Periods)
                .First()
                .Sessions;

            if (!expected.Any(x => SameName(x.Name, name)))
                throw new InvalidOperationException("Buổi thi không đúng cấu trúc chuẩn.");

            var current = await _repo.GetAllByPeriodAsync(periodId);
            if (current.Any(x => SameName(x.Name, name)))
                throw new InvalidOperationException("Buổi thi đã tồn tại.");

            var entity = new ExamSession
            {
                Name = name
            };

            await _repo.AddAsync(periodId, entity);
        }

        public async Task UpdateAsync(SessionDto dto)
        {
            dto.Name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Vui lòng nhập tên buổi thi.");

            var validNames = DefaultDataBuilder.Build().Semesters
                .SelectMany(x => x.Periods)
                .SelectMany(x => x.Sessions)
                .Select(x => x.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            if (!validNames.Any(x => SameName(x, dto.Name)))
                throw new InvalidOperationException("Buổi thi không đúng cấu trúc chuẩn.");

            var session = await _repo.GetByIdAsync(dto.Id);
            if (session != null)
            {
                var current = await _repo.GetAllByPeriodAsync(session.PeriodId);
                if (current.Any(x => x.Id != dto.Id && SameName(x.Name, dto.Name)))
                    throw new InvalidOperationException("Buổi thi đã tồn tại.");
            }

            await _repo.UpdateAsync(dto.Id, dto.Name);
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }

        private static bool SameName(string? current, string? expected)
        {
            return string.Equals(current?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
