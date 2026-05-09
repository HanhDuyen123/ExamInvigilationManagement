using ExamInvigilationManagement.Domain.Enums;

namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class SemesterOptionDto
    {
        public SemesterType Type { get; set; }
        public bool Selected { get; set; }

        public List<ExamPeriodOptionDto> Periods { get; set; } = new();
    }
}
