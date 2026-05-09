using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.Position
{
    public class PositionDto
    {
        public byte PositionId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chức vụ.")]
        [StringLength(50, ErrorMessage = "Tên chức vụ tối đa 50 ký tự.")]
        public string PositionName { get; set; } = string.Empty;
    }
}
