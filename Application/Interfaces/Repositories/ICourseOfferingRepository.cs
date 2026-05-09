using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface ICourseOfferingRepository
    {
        Task<List<CourseOffering>> GetAllAsync();
        Task<CourseOffering?> GetByIdAsync(int id);
        Task AddAsync(CourseOffering entity);
        Task UpdateAsync(CourseOffering entity);
        Task DeleteAsync(int id);

        Task<bool> ExistsByIdAsync(int id);
        Task<bool> ExistsByUniqueKeyAsync(int semesterId, string subjectId, string className, string groupNumber, int? excludeId = null);
        Task<bool> HasExamSchedulesAsync(int offeringId);

        Task<bool> UserExistsAsync(int userId);
        Task<bool> IsUserActiveLecturerAsync(int userId);
        Task<int?> GetUserFacultyIdAsync(int userId);

        Task<bool> SubjectExistsAsync(string subjectId);
        Task<int?> GetSubjectFacultyIdAsync(string subjectId);

        Task<bool> SemesterExistsAsync(int semesterId);
        Task<int?> GetSemesterAcademyYearIdAsync(int semesterId);
    }
}