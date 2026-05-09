using ExamInvigilationManagement.Application.DTOs.Admin.Position;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class PositionService : IPositionService
    {
        private readonly IPositionRepository _repo;

        public PositionService(IPositionRepository repo)
        {
            _repo = repo;
        }

        public async Task<PagedResult<PositionDto>> GetPagedAsync(string? keyword, int pageIndex, int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = NormalizeName(keyword);
                data = data
                    .Where(x => NormalizeName(x.Name).Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var total = data.Count;

            var items = data
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PositionDto
                {
                    PositionId = x.Id,
                    PositionName = x.Name
                })
                .ToList();

            return new PagedResult<PositionDto>
            {
                Items = items,
                TotalCount = total,
                Page = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<List<PositionDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new PositionDto
            {
                PositionId = x.Id,
                PositionName = x.Name
            }).ToList();
        }

        public async Task<PositionDto?> GetByIdAsync(byte id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new PositionDto
            {
                PositionId = x.Id,
                PositionName = x.Name
            };
        }

        public async Task CreateAsync(PositionDto dto)
        {
            var name = NormalizeName(dto.PositionName);
            ValidateName(name);

            if (await _repo.ExistsByNameAsync(name))
                throw new InvalidOperationException("Tên chức vụ đã tồn tại.");

            await _repo.AddAsync(new Position
            {
                Name = name
            });
        }

        public async Task UpdateAsync(PositionDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.PositionId);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy chức vụ cần cập nhật.");

            var name = NormalizeName(dto.PositionName);
            ValidateName(name);

            if (await _repo.ExistsByNameAsync(name, excludeId: dto.PositionId))
                throw new InvalidOperationException("Tên chức vụ đã tồn tại.");

            await _repo.UpdateAsync(new Position
            {
                Id = dto.PositionId,
                Name = name
            });
        }

        public async Task DeleteAsync(byte id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy chức vụ cần xóa.");

            if (await _repo.HasInformationAsync(id))
                throw new InvalidOperationException("Không thể xóa chức vụ vì đang có thông tin cá nhân sử dụng chức vụ này.");

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
                throw new InvalidOperationException("Vui lòng nhập tên chức vụ.");

            if (name.Length > 50)
                throw new InvalidOperationException("Tên chức vụ tối đa 50 ký tự.");

            if (string.IsNullOrWhiteSpace(name.Trim()))
                throw new InvalidOperationException("Tên chức vụ không được chỉ chứa khoảng trắng.");
        }
    }
}