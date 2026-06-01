using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.User
{
    public class LecturerManagementDto
    {
        public int Id { get; set; }
        public int InformationId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã giảng viên.")]
        [StringLength(8, MinimumLength = 3, ErrorMessage = "Mã giảng viên từ 3 đến 8 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9_]+$", ErrorMessage = "Mã giảng viên chỉ gồm chữ, số và dấu gạch dưới, không có khoảng trắng.")]
        public string UserName { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 100 ký tự.")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50, ErrorMessage = "Họ tối đa 50 ký tự.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50, ErrorMessage = "Tên tối đa 50 ký tự.")]
        public string FirstName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa/viện.")]
        public int? FacultyId { get; set; }
        public string? FacultyName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "Số điện thoại tối đa 10 ký tự.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số.")]
        public string? Phone { get; set; }

        public DateTime? Dob { get; set; }

        [Range(1, 255, ErrorMessage = "Vui lòng chọn chức vụ.")]
        public byte PositionId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
