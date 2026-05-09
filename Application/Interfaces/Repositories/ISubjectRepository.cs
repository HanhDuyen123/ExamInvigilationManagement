using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface ISubjectRepository
    {
        Task<PagedResult<Subject>> GetPagedAsync(
            string? id,
            string? name,
            byte? credit,
            int? facultyId,
            int page,
            int pageSize);

        Task<List<Subject>> GetAllAsync();
        Task<Subject?> GetByIdAsync(string id);

        Task<bool> ExistsByIdAsync(string id);
        Task<bool> FacultyExistsAsync(int facultyId);
        Task<bool> HasCourseOfferingsAsync(string subjectId);

        Task AddAsync(Subject entity);
        Task UpdateAsync(Subject entity);
        Task DeleteAsync(string id);
    }
}