using ExamInvigilationManagement.Application.DTOs.Admin.Position;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IPositionService
    {
        Task<PagedResult<PositionDto>> GetPagedAsync(string? keyword, int pageIndex, int pageSize);
        Task<List<PositionDto>> GetAllAsync();
        Task<PositionDto?> GetByIdAsync(byte id);
        Task CreateAsync(PositionDto dto);
        Task UpdateAsync(PositionDto dto);
        Task DeleteAsync(byte id);
    }
}