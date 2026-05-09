using ExamInvigilationManagement.Application.DTOs.Admin.User;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IAdminUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);

        Task<PagedResult<User>> GetPagedAsync(
            string? keyword,
            int? roleId,
            int? informationId,
            int? facultyId,
            bool? isActive,
            int page,
            int pageSize);

        Task<bool> ExistsByUserNameAsync(string userName, int? excludeId = null);
        Task<bool> RoleExistsAsync(byte roleId);
        Task<bool> InformationExistsAsync(int informationId);
        Task<bool> FacultyExistsAsync(int facultyId);

        Task<string?> GetRoleNameByIdAsync(byte roleId);

        Task<bool> ExistsInformationInRoleAsync(
            int informationId,
            byte roleId,
            int? excludeUserId = null);

        Task<bool> HasDependenciesAsync(int userId);

        Task AddAsync(User entity);
        Task UpdateAsync(User entity);
        Task DeleteAsync(int id);
    }
}