using ExamInvigilationManagement.Application.DTOs.Admin.Faculty;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IFacultyService
    {
        Task<PagedResult<FacultyDto>> GetPagedAsync(string? keyword, int page, int pageSize);

        Task<List<FacultyDto>> GetAllAsync();

        Task<FacultyDto?> GetByIdAsync(int id);

        Task CreateAsync(FacultyDto dto);
        Task UpdateAsync(FacultyDto dto);
        Task DeleteAsync(int id);
    }
}
