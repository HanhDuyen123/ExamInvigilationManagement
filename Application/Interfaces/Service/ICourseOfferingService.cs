using ExamInvigilationManagement.Application.DTOs.Admin.CourseOffering;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface ICourseOfferingService
    {
        Task<PagedResult<CourseOfferingDto>> GetPagedAsync(
            string? subjectId,
            int? userId,
            int? semesterType,
            string? className,
            string? groupNumber,
            int page,
            int pageSize);

        Task<CourseOfferingDto?> GetByIdAsync(int id);
        Task<List<CourseOfferingDto>> GetAllAsync();

        Task CreateAsync(CourseOfferingDto dto);
        Task UpdateAsync(CourseOfferingDto dto);
        Task DeleteAsync(int id);
    }
}
