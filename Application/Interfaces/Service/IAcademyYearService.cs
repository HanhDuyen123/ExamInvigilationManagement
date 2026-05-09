using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IAcademyYearService
    {
        //Task<PagedResult<AcademyYearDto>> GetPagedAsync(string? keyword, int page, int pageSize);
        Task<PagedResult<AcademyYearDto>> GetPagedAsync(
            string? keyword,
            int? semesterType,
            int page,
            int pageSize);
        Task<List<AcademyYearDto>> GetAllAsync();
        Task<AcademyYearDto?> GetByIdAsync(int id);
        Task<AcademyYearDetailDto?> GetDetailAsync(int id);
        Task CreateAsync(AcademyYearDto dto);
        Task CreateAsync(CreateAcademyYearDto dto);
        Task UpdateAsync(AcademyYearDto dto);
        Task DeleteAsync(int id);
    }
}