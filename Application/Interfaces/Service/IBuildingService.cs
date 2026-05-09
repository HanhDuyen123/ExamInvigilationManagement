using ExamInvigilationManagement.Application.DTOs.Admin.Building;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IBuildingService
    {
        Task<PagedResult<BuildingDto>> GetPagedAsync(string? keyword, int page, int pageSize);
        Task<List<BuildingDto>> GetAllAsync();
        Task<BuildingDto?> GetByIdAsync(string id);
        Task CreateAsync(BuildingDto dto);
        Task UpdateAsync(BuildingDto dto);
        Task DeleteAsync(string id);
    }
}
