using ExamInvigilationManagement.Application.DTOs.Admin.Role;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class RoleService : IRoleService
    {
        private static readonly HashSet<string> ProtectedRoleNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",
            "Giảng viên",
            "Thư ký khoa",
            "Trưởng khoa"
        };

        private readonly IRoleRepository _repo;

        public RoleService(IRoleRepository repo)
        {
            _repo = repo;
        }

        public async Task<PagedResult<RoleDto>> GetPagedAsync(string? keyword, int page, int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data
                    .Where(x => x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var total = data.Count;

            var items = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RoleDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsProtected = IsProtectedRoleName(x.Name)
                })
                .ToList();

            return new PagedResult<RoleDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<RoleDto?> GetByIdAsync(byte id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new RoleDto
            {
                Id = x.Id,
                Name = x.Name,
                IsProtected = IsProtectedRoleName(x.Name)
            };
        }

        public async Task<List<RoleDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new RoleDto
            {
                Id = x.Id,
                Name = x.Name,
                IsProtected = IsProtectedRoleName(x.Name)
            }).ToList();
        }

        public async Task CreateAsync(RoleDto dto)
        {
            var name = NormalizeName(dto.Name);
            ValidateName(name);

            if (IsProtectedRoleName(name))
                throw new InvalidOperationException("Không được tạo vai trò trùng với vai trò hệ thống.");

            if (await _repo.ExistsByNameAsync(name))
                throw new InvalidOperationException("Tên vai trò đã tồn tại.");

            await _repo.AddAsync(new Role
            {
                Name = name
            });
        }

        public async Task UpdateAsync(RoleDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy vai trò cần cập nhật.");

            var name = NormalizeName(dto.Name);
            ValidateName(name);

            if (existing.Name != null && IsProtectedRoleName(existing.Name) &&
                !string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Không được đổi tên vai trò hệ thống.");
            }

            if (await _repo.ExistsByNameAsync(name, excludeId: dto.Id))
                throw new InvalidOperationException("Tên vai trò đã tồn tại.");

            await _repo.UpdateAsync(new Role
            {
                Id = dto.Id,
                Name = name
            });
        }

        public async Task DeleteAsync(byte id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy vai trò cần xóa.");

            if (IsProtectedRoleName(existing.Name))
                throw new InvalidOperationException("Không thể xóa vai trò hệ thống.");

            if (await _repo.HasUsersAsync(id))
                throw new InvalidOperationException("Không thể xóa vai trò vì đang có tài khoản sử dụng vai trò này.");

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
                throw new InvalidOperationException("Vui lòng nhập tên vai trò.");

            if (name.Length > 50)
                throw new InvalidOperationException("Tên vai trò tối đa 50 ký tự.");

            if (string.IsNullOrWhiteSpace(name.Trim()))
                throw new InvalidOperationException("Tên vai trò không được chỉ chứa khoảng trắng.");
        }

        private static bool IsProtectedRoleName(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            return ProtectedRoleNames.Contains(roleName.Trim());
        }
    }
}