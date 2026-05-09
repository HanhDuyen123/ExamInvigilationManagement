using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.Building
{
    public class BuildingDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mã giảng đường.")]
        [StringLength(10, ErrorMessage = "Mã giảng đường tối đa 10 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Mã giảng đường chỉ được chứa chữ cái và số, không có khoảng trắng.")]
        public string BuildingId { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập tên giảng đường.")]
        [StringLength(50, ErrorMessage = "Tên giảng đường tối đa 50 ký tự.")]
        public string BuildingName { get; set; } = null!;
    }
}