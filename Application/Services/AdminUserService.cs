using ExamInvigilationManagement.Application.DTOs.Admin.User;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class AdminUserService : IAdminUserService
    {
        private const string RoleAdmin = "Admin";

        private readonly IAdminUserRepository _repo;
        private readonly IPasswordService _passwordService;

        public AdminUserService(IAdminUserRepository repo, IPasswordService passwordService)
        {
            _repo = repo;
            _passwordService = passwordService;
        }

        public async Task<PagedResult<UserDto>> GetPagedAsync(string? keyword, int page, int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data.Where(x =>
                    x.UserName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    $"{x.Information?.LastName} {x.Information?.FirstName}".Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var total = data.Count;

            var items = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserDto
                {
                    Id = x.Id,
                    UserName = x.UserName,
                    RoleId = x.RoleId,
                    InformationId = x.InformationId,
                    FacultyId = x.FacultyId,
                    RoleName = x.Role?.Name,
                    FullName = x.Information != null
                        ? $"{x.Information.LastName} {x.Information.FirstName}"
                        : string.Empty,
                    Email = x.Information?.Email,
                    FacultyName = x.Faculty?.Name,
                    IsActive = x.IsActive
                })
                .ToList();

            return new PagedResult<UserDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new UserDto
            {
                Id = x.Id,
                RoleId = x.RoleId,
                InformationId = x.InformationId,
                FacultyId = x.FacultyId,
                UserName = x.UserName,
                IsActive = x.IsActive,
                RoleName = x.Role?.Name,
                FullName = x.Information != null
                    ? $"{x.Information.LastName} {x.Information.FirstName}" 
                    : string.Empty,
                Email = x.Information?.Email,
                FacultyName = x.Faculty?.Name
            };
        }

        public async Task<PagedResult<UserDto>> GetPagedAsync(
            string? keyword,
            int? roleId,
            int? informationId,
            int? facultyId,
            bool? isActive,
            int page,
            int pageSize)
        {
            var paged = await _repo.GetPagedAsync(
                keyword, roleId, informationId, facultyId, isActive, page, pageSize);

            var items = paged.Items.Select(x => new UserDto
            {
                Id = x.Id,
                UserName = x.UserName,
                RoleId = x.RoleId,
                InformationId = x.InformationId,
                FacultyId = x.FacultyId,
                RoleName = x.Role?.Name,
                FullName = x.Information != null
                    ? $"{x.Information.LastName} {x.Information.FirstName}"
                    : string.Empty,
                Email = x.Information?.Email,
                FacultyName = x.Faculty?.Name,
                IsActive = x.IsActive
            }).ToList();

            return new PagedResult<UserDto>
            {
                Items = items,
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }

        public async Task CreateAsync(UserDto dto)
        {
            var normalized = Normalize(dto);

            await ValidateAsync(normalized, isUpdate: false);

            if (string.IsNullOrWhiteSpace(normalized.Password))
                throw new InvalidOperationException("Vui lòng nhập mật khẩu.");

            if (await _repo.ExistsByUserNameAsync(normalized.UserName))
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

            await EnsureFacultyRuleAsync(normalized.RoleId, normalized.FacultyId);

            if (await _repo.ExistsInformationInRoleAsync(normalized.InformationId, normalized.RoleId))
                throw new InvalidOperationException("Thông tin cá nhân này đã được gán cho một tài khoản khác trong cùng vai trò.");

            await _repo.AddAsync(new User
            {
                RoleId = normalized.RoleId,
                InformationId = normalized.InformationId,
                FacultyId = normalized.FacultyId,
                UserName = normalized.UserName,
                PasswordHash = _passwordService.HashPassword(normalized.Password),
                IsActive = normalized.IsActive
            });
        }

        public async Task UpdateAsync(UserDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản cần cập nhật.");

            var normalized = Normalize(dto);

            await ValidateAsync(normalized, isUpdate: true);

            if (await _repo.ExistsByUserNameAsync(normalized.UserName, excludeId: normalized.Id))
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

            await EnsureFacultyRuleAsync(normalized.RoleId, normalized.FacultyId);

            if (await _repo.ExistsInformationInRoleAsync(normalized.InformationId, normalized.RoleId, excludeUserId: normalized.Id))
                throw new InvalidOperationException("Thông tin cá nhân này đã được gán cho một tài khoản khác trong cùng vai trò.");

            await _repo.UpdateAsync(new User
            {
                Id = normalized.Id,
                RoleId = normalized.RoleId,
                InformationId = normalized.InformationId,
                FacultyId = normalized.FacultyId,
                UserName = normalized.UserName,
                IsActive = normalized.IsActive
            });
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản cần xóa.");

            if (await _repo.HasDependenciesAsync(id))
                throw new InvalidOperationException("Không thể xóa tài khoản vì đã phát sinh dữ liệu nghiệp vụ liên quan. Hãy vô hiệu hóa tài khoản thay thế.");

            await _repo.DeleteAsync(id);
        }

        private async Task ValidateAsync(UserDto dto, bool isUpdate)
        {
            if (dto.RoleId <= 0 || !await _repo.RoleExistsAsync(dto.RoleId))
                throw new InvalidOperationException("Vui lòng chọn vai trò hợp lệ.");

            if (dto.InformationId <= 0 || !await _repo.InformationExistsAsync(dto.InformationId))
                throw new InvalidOperationException("Vui lòng chọn thông tin cá nhân hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.UserName))
                throw new InvalidOperationException("Vui lòng nhập tên đăng nhập.");

            if (dto.UserName.Length < 3 || dto.UserName.Length > 8)
                throw new InvalidOperationException("Tên đăng nhập từ 3 đến 8 ký tự.");

            if (!Regex.IsMatch(dto.UserName, @"^[A-Za-z0-9_]+$"))
                throw new InvalidOperationException("Tên đăng nhập chỉ gồm chữ, số và dấu gạch dưới, không có khoảng trắng.");

            if (dto.FacultyId.HasValue && dto.FacultyId.Value > 0 && !await _repo.FacultyExistsAsync(dto.FacultyId.Value))
                throw new InvalidOperationException("Khoa đã chọn không tồn tại.");

            if (!isUpdate && string.IsNullOrWhiteSpace(dto.Password))
                throw new InvalidOperationException("Vui lòng nhập mật khẩu.");
        }

        private async Task EnsureFacultyRuleAsync(byte roleId, int? facultyId)
        {
            var roleName = await _repo.GetRoleNameByIdAsync(roleId);
            if (string.IsNullOrWhiteSpace(roleName))
                throw new InvalidOperationException("Không xác định được vai trò.");

            if (roleName.Equals(RoleAdmin, StringComparison.OrdinalIgnoreCase))
            {
                if (facultyId.HasValue)
                    throw new InvalidOperationException("Tài khoản Admin không cần gắn khoa.");
            }
            else
            {
                if (!facultyId.HasValue || facultyId.Value <= 0)
                    throw new InvalidOperationException("Vui lòng chọn khoa cho tài khoản này.");
            }
        }

        private static UserDto Normalize(UserDto dto)
        {
            dto.UserName = (dto.UserName ?? string.Empty).Trim();
            dto.Password = string.IsNullOrWhiteSpace(dto.Password) ? null : dto.Password.Trim();
            return dto;
        }
    }
}
