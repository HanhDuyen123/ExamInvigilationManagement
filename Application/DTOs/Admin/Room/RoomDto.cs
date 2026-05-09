using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.Room
{
    public class RoomDto
    {
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giảng đường.")]
        public string BuildingId { get; set; } = null!;
        public string? BuildingName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phòng.")]
        [StringLength(50, ErrorMessage = "Tên phòng tối đa 50 ký tự.")]
        public string RoomName { get; set; } = null!;

        [Range(1, 500, ErrorMessage = "Sức chứa phải từ 1 đến 500.")]
        public int? Capacity { get; set; }
    }
}
