using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.Subject
{
    public class SubjectDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mã môn học.")]
        [StringLength(10, ErrorMessage = "Mã môn học tối đa 10 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Mã môn học chỉ được chứa chữ cái và số, không có khoảng trắng.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn khoa.")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa.")]
        public int? FacultyId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên môn học.")]
        [StringLength(100, ErrorMessage = "Tên môn học tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số tín chỉ.")]
        [Range(1, 20, ErrorMessage = "Số tín chỉ phải từ 1 đến 20.")]
        public byte? Credit { get; set; }

        public string? FacultyName { get; set; }
    }
}