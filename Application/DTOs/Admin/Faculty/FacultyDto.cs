using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.Faculty
{
    public class FacultyDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khoa.")]
        [StringLength(50, ErrorMessage = "Tên khoa tối đa 50 ký tự.")]
        public string Name { get; set; } = string.Empty;
    }
}
