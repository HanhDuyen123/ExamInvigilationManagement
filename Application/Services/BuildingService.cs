using ExamInvigilationManagement.Application.DTOs.Admin.Building;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly IBuildingRepository _repo;

        public BuildingService(IBuildingRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<BuildingDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new BuildingDto
            {
                BuildingId = x.Id,
                BuildingName = x.Name
            }).ToList();
        }

        public async Task<PagedResult<BuildingDto>> GetPagedAsync(string? keyword, int page, int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data.Where(x =>
                    x.Id.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var total = data.Count;

            var items = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BuildingDto
                {
                    BuildingId = x.Id,
                    BuildingName = x.Name
                }).ToList();

            return new PagedResult<BuildingDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<BuildingDto?> GetByIdAsync(string id)
        {
            id = NormalizeId(id);

            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return null;

            return new BuildingDto
            {
                BuildingId = entity.Id,
                BuildingName = entity.Name
            };
        }

        public async Task CreateAsync(BuildingDto dto)
        {
            var id = NormalizeId(dto.BuildingId);
            var name = NormalizeName(dto.BuildingName);

            ValidateId(id);
            ValidateName(name);

            if (await _repo.ExistsByIdAsync(id))
                throw new InvalidOperationException("Mã giảng đường đã tồn tại.");

            if (await _repo.ExistsByNameAsync(name))
                throw new InvalidOperationException("Tên giảng đường đã tồn tại.");

            await _repo.AddAsync(new Domain.Entities.Building
            {
                Id = id,
                Name = name
            });
        }

        public async Task UpdateAsync(BuildingDto dto)
        {
            var id = NormalizeId(dto.BuildingId);
            var name = NormalizeName(dto.BuildingName);

            ValidateId(id);
            ValidateName(name);

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy giảng đường cần cập nhật.");

            if (await _repo.ExistsByNameAsync(name, excludeId: id))
                throw new InvalidOperationException("Tên giảng đường đã tồn tại.");

            await _repo.UpdateAsync(new Domain.Entities.Building
            {
                Id = id,
                Name = name
            });
        }

        public async Task DeleteAsync(string id)
        {
            id = NormalizeId(id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy giảng đường cần xóa.");

            if (await _repo.HasRoomsAsync(id))
                throw new InvalidOperationException("Không thể xóa giảng đường vì vẫn còn phòng thuộc giảng đường này.");

            await _repo.DeleteAsync(id);
        }

        private static string NormalizeId(string? id)
        {
            return (id ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizeName(string? name)
        {
            var value = (name ?? string.Empty).Trim();
            value = Regex.Replace(value, @"\s+", " ");
            return value;
        }

        private static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Vui lòng nhập mã giảng đường.");

            if (id.Length > 10)
                throw new InvalidOperationException("Mã giảng đường tối đa 10 ký tự.");

            if (!Regex.IsMatch(id, @"^[A-Z0-9]+$"))
                throw new InvalidOperationException("Mã giảng đường chỉ được chứa chữ cái và số, không có khoảng trắng.");

            if (string.IsNullOrWhiteSpace(id.Trim()))
                throw new InvalidOperationException("Mã giảng đường không được chỉ chứa khoảng trắng.");
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Vui lòng nhập tên giảng đường.");

            if (name.Length > 50)
                throw new InvalidOperationException("Tên giảng đường tối đa 50 ký tự.");

            if (string.IsNullOrWhiteSpace(name.Trim()))
                throw new InvalidOperationException("Tên giảng đường không được chỉ chứa khoảng trắng.");
        }
    }
}