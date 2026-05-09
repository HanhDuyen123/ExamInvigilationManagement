using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.User
{
    public class UserDto
    {
        public int Id { get; set; }

        [Range(1, byte.MaxValue, ErrorMessage = "Vui lòng chọn vai trò.")]
        public byte RoleId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thông tin cá nhân.")]
        public int InformationId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa.")]
        public int? FacultyId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(8, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 8 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ, số và dấu gạch dưới, không có khoảng trắng.")]
        public string UserName { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 100 ký tự.")]
        public string? Password { get; set; }

        public bool IsActive { get; set; }

        public string? RoleName { get; set; }
        public string? FullName { get; set; }
        public string? FacultyName { get; set; }
        public string? Email { get; set; }
    }
}
