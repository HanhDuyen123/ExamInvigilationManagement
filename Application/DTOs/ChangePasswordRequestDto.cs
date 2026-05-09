using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs
{
    public class ChangePasswordRequestDto
    {
        public int UserId { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        public string CurrentPassword { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).+$", ErrorMessage = "Mật khẩu phải gồm chữ cái, số và ký tự đặc biệt.")]
        public string NewPassword { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
