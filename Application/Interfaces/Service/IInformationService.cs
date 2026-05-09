using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Application.DTOs.Admin.Information;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IInformationService
    {
        Task<PagedResult<InformationDto>> GetPagedAsync(
            string? name, string? email, string? gender, DateTime? dob, byte? positionId, int page, int pageSize);
        Task<InformationDto?> GetByIdAsync(int id);
        Task<List<InformationDto>> GetAllAsync();

        Task CreateAsync(InformationDto dto);
        Task UpdateAsync(InformationDto dto);
        Task DeleteAsync(int id);
    }
}