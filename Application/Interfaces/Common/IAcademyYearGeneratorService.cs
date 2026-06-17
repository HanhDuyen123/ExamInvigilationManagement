using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Common
{
    public interface IAcademyYearGeneratorService
    {
        Task GenerateAsync(AcademyYear year, List<SemesterOptionDto> options);
    }
}
