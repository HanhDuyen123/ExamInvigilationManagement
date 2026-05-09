using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Application.DTOs.Admin.Role;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IRoleService
    {
        Task<PagedResult<RoleDto>> GetPagedAsync(string? keyword, int page, int pageSize);
        Task<RoleDto?> GetByIdAsync(byte id);
        Task<List<RoleDto>> GetAllAsync();
        Task CreateAsync(RoleDto dto);
        Task UpdateAsync(RoleDto dto);
        Task DeleteAsync(byte id);
    }
}