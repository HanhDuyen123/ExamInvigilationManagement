using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear
{
    public class AcademyYearDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập năm học.")]
        [StringLength(20, ErrorMessage = "Năm học tối đa 20 ký tự.")]
        public string Name { get; set; } = string.Empty;
        public int SemesterCount { get; set; }
        public int PeriodCount { get; set; }
        public int SessionCount { get; set; }
        public int SlotCount { get; set; }
        public bool IsComplete { get; set; }
    }
}
