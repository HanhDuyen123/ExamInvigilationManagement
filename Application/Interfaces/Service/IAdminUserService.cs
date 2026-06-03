using ExamInvigilationManagement.Application.DTOs.Admin.User;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IAdminUserService
    {
        Task<PagedResult<UserDto>> GetPagedAsync(string? keyword, int page, int pageSize);

        Task<UserDto?> GetByIdAsync(int id);
        Task<PagedResult<UserDto>> GetPagedAsync(
            string? keyword,
            int? roleId,
            int? informationId,
            int? facultyId,
            bool? isActive,
            int page,
            int pageSize);

        Task CreateAsync(UserDto dto);
        Task UpdateAsync(UserDto dto);
        Task DeleteAsync(int id);
        Task SetActiveAsync(int id, bool isActive);

        Task<PagedResult<LecturerManagementDto>> GetLecturersPagedAsync(string? keyword, int? facultyId, bool? isActive, int page, int pageSize);
        Task<LecturerManagementDto?> GetLecturerByIdAsync(int id, int? facultyScopeId = null);
        Task CreateLecturerAsync(LecturerManagementDto dto, int? facultyScopeId = null);
        Task UpdateLecturerAsync(LecturerManagementDto dto, int? facultyScopeId = null);
    }
}
