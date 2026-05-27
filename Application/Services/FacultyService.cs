using ExamInvigilationManagement.Application.DTOs.Admin.Faculty;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class FacultyService : IFacultyService
    {
        private readonly IFacultyRepository _repo;

        public FacultyService(IFacultyRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<FacultyDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new FacultyDto
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
        }

        public async Task<PagedResult<FacultyDto>> GetPagedAsync(string? keyword, int page, int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = NormalizeComparableName(keyword);
                data = data
                    .Where(x => NormalizeComparableName(x.Name).Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var total = data.Count;

            var items = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FacultyDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();

            return new PagedResult<FacultyDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<FacultyDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new FacultyDto
            {
                Id = x.Id,
                Name = x.Name
            };
        }

        public async Task CreateAsync(FacultyDto dto)
        {
            var name = NormalizeName(dto.Name);
            ValidateName(name);

            var existing = await _repo.GetAllAsync();
            if (existing.Any(x => NormalizeComparableName(x.Name) == NormalizeComparableName(name)))
                throw new InvalidOperationException("Tên khoa đã tồn tại.");

            await _repo.AddAsync(new Faculty
            {
                Name = name
            });
        }

        public async Task UpdateAsync(FacultyDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy khoa cần cập nhật.");

            var name = NormalizeName(dto.Name);
            ValidateName(name);

            var all = await _repo.GetAllAsync();
            if (all.Any(x => x.Id != dto.Id && NormalizeComparableName(x.Name) == NormalizeComparableName(name)))
                throw new InvalidOperationException("Tên khoa đã tồn tại.");

            await _repo.UpdateAsync(new Faculty
            {
                Id = dto.Id,
                Name = name
            });
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy khoa cần xóa.");

            if (await _repo.HasUsersAsync(id))
                throw new InvalidOperationException("Không thể xóa khoa vì đang có tài khoản thuộc khoa này.");

            if (await _repo.HasSubjectsAsync(id))
                throw new InvalidOperationException("Không thể xóa khoa vì đang có môn học thuộc khoa này.");

            await _repo.DeleteAsync(id);
        }

        private static string NormalizeName(string? name)
        {
            var value = (name ?? string.Empty).Trim();
            value = Regex.Replace(value, @"\s+", " ");
            return value;
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Vui lòng nhập tên khoa.");

            if (name.Length > 100)
                throw new InvalidOperationException("Tên khoa tối đa 100 ký tự.");

            if (string.IsNullOrWhiteSpace(name.Trim()))
                throw new InvalidOperationException("Tên khoa không được chỉ chứa khoảng trắng.");
        }

        private static string NormalizeComparableName(string? name)
        {
            var value = NormalizeName(name).ToLowerInvariant();
            value = Regex.Replace(value, @"^(khoa|trung tâm|trung tam|trường|truong)\s+", string.Empty, RegexOptions.IgnoreCase);
            return value;
        }
    }
}
