using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.ExamSchedule
{
    public class ExamFormatDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã hình thức thi.")]
        [StringLength(20, ErrorMessage = "Mã hình thức thi tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên hình thức thi.")]
        [StringLength(100, ErrorMessage = "Tên hình thức thi tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string DisplayName => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} - {Name}";
    }
}
