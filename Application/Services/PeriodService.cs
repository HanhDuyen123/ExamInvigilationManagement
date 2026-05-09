using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Repositories;

namespace ExamInvigilationManagement.Application.Services
{
    public class PeriodService : IPeriodService
    {
        private readonly IPeriodRepository _repo;
        private readonly ISemesterRepository _semesterRepo;

        public PeriodService(IPeriodRepository repo, ISemesterRepository semesterRepo)
        {
            _repo = repo;
            _semesterRepo = semesterRepo;
        }
        public async Task<List<PeriodDto>> GetAllBySemesterAsync(int semesterId)
        {
            var periods = await _repo.GetAllBySemesterAsync(semesterId);
            return periods.Select(p => new PeriodDto
            {
                Id = p.Id,
                Name = p.Name
            }).ToList();
        }
        public async Task<string> GetExamPeriodNameAsync(int periodId)
        {
            var period = await _repo.GetByIdAsync(periodId);
            return period?.Name ?? string.Empty;
        }
        public async Task AddAsync(int semesterId, string name)
        {
            name = (name ?? string.Empty).Trim();
            var semester = await _semesterRepo.GetByIdAsync(semesterId);
            var semesterType = SemesterHelper.ToType(semester?.Name ?? string.Empty);

            if (semester == null || semesterType == null)
                throw new InvalidOperationException("Học kỳ không hợp lệ.");

            var expected = DefaultDataBuilder.Build().Semesters
                .First(x => x.Type == semesterType)
                .Periods;

            if (!expected.Any(x => SameName(x.Name, name)))
                throw new InvalidOperationException("Đợt thi không đúng cấu trúc chuẩn.");

            var current = await _repo.GetAllBySemesterAsync(semesterId);
            if (current.Any(x => SameName(x.Name, name)))
                throw new InvalidOperationException("Đợt thi đã tồn tại.");

            var entity = new ExamPeriod
            {
                Name = name
            };

            await _repo.AddAsync(semesterId, entity);
        }

        public async Task UpdateAsync(PeriodDto dto)
        {
            dto.Name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Vui lòng nhập tên đợt thi.");

            var period = await _repo.GetByIdAsync(dto.Id);
            var semester = period == null ? null : await _semesterRepo.GetByIdAsync(period.SemesterId);
            var semesterType = SemesterHelper.ToType(semester?.Name ?? string.Empty);
            var validNames = semesterType == null
                ? Enumerable.Empty<ExamPeriodOptionDto>()
                : DefaultDataBuilder.Build().Semesters.First(x => x.Type == semesterType).Periods;

            if (!validNames.Any(x => SameName(x.Name, dto.Name)))
                throw new InvalidOperationException("Đợt thi không đúng cấu trúc chuẩn.");

            if (period != null)
            {
                var current = await _repo.GetAllBySemesterAsync(period.SemesterId);
                if (current.Any(x => x.Id != dto.Id && SameName(x.Name, dto.Name)))
                    throw new InvalidOperationException("Đợt thi đã tồn tại.");
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
