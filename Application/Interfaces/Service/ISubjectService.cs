using ExamInvigilationManagement.Application.DTOs.Admin.Subject;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface ISubjectService
    {
        //Task<PagedResult<SubjectDto>> GetPagedAsync(string? keyword, int page, int pageSize);
        Task<PagedResult<SubjectDto>> GetPagedAsync(
            string? id,
            string? name,
            byte? credit,
            int? facultyId,
            int page,
            int pageSize);
        Task<List<SubjectDto>> GetAllAsync();
        Task<SubjectDto?> GetByIdAsync(string id);

        Task CreateAsync(SubjectDto dto);
        Task UpdateAsync(SubjectDto dto);
        Task DeleteAsync(string id);
    }
}