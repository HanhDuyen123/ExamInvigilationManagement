using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class CreateAcademyYearDto
    {
        [Required(ErrorMessage = "Vui lòng nhập năm học.")]
        [StringLength(20, ErrorMessage = "Năm học tối đa 20 ký tự.")]
        public string Name { get; set; } = null!;
        public bool AutoGenerate { get; set; }

        public List<SemesterOptionDto> Semesters { get; set; } = new();
    }
}
