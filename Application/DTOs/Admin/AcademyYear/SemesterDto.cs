using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Enums;

namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class SemesterDto
    {
        public int Id { get; set; }
        public int AcademyYearId { get; set; }
        public string Name { get; set; }
        public SemesterType? Type =>
    SemesterHelper.ToType(Name);

        public List<PeriodDto> Periods { get; set; } = new();
        public string? AcademicYear { get; set; }
    }
}
