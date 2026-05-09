using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.Information
{
    public class InformationDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50, ErrorMessage = "Tên tối đa 50 ký tự.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50, ErrorMessage = "Họ tối đa 50 ký tự.")]
        public string LastName { get; set; } = string.Empty;

        public DateTime? Dob { get; set; }

        [StringLength(10, ErrorMessage = "Số điện thoại tối đa 10 ký tự.")]
        public string? Phone { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Avt { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [Range(1, 255, ErrorMessage = "Vui lòng chọn chức vụ.")]
        public byte PositionId { get; set; }

        public string? PositionName { get; set; }
    }
}