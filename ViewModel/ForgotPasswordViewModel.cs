using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.ViewModel
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Username không được để trống")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
    }
}