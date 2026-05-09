using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Domain.Enums;

namespace ExamInvigilationManagement.Application.Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _repo;
        private readonly IAcademyYearRepository _yearRepo;

        public SemesterService(
            ISemesterRepository repo,
            IAcademyYearRepository yearRepo)
        {
            _repo = repo;
            _yearRepo = yearRepo;
        }

        public async Task<List<SemesterDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new SemesterDto
            {
                Id = x.Id,
                Name = x.Name,
                AcademyYearId = x.AcademyYearId,
                AcademicYear = x.AcademyYear != null
                    ? x.AcademyYear.Name 
                    : null
            }).ToList();
        }
        public async Task<string> GetSemesterNameAsync(int semesterId)
        {
            var semester = await _repo.GetByIdAsync(semesterId);
            return semester?.Name ?? string.Empty;
        }
        public async Task AddAsync(int academyYearId, SemesterType type)
        {
            var year = await _yearRepo.GetDetailAsync(academyYearId);

            if (year == null)
                throw new InvalidOperationException("Không tìm thấy năm học.");

            // ✅ check bằng helper
            var hasType = year.Semesters.Any(s =>
                SemesterHelper.ToType(s.Name) == type
            );

            if (hasType)
                throw new InvalidOperationException("Học kỳ đã tồn tại.");

            if (year.Semesters.Count >= 3)
                throw new InvalidOperationException("Đã đủ 3 học kỳ.");

            var entity = new Semester
            {
                Name = SemesterHelper.ToName(type)
            };

            await _repo.AddAsync(academyYearId, entity);
        }

        public async Task UpdateAsync(SemesterDto dto)
        {
            dto.Name = (dto.Name ?? string.Empty).Trim();
            if (SemesterHelper.ToType(dto.Name) == null)
                throw new InvalidOperationException("Học kỳ không đúng cấu trúc chuẩn.");

            await _repo.UpdateAsync(dto.Id, dto.Name);
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }
    }
}
