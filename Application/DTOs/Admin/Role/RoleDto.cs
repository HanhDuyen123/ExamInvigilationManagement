using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.Role
{
    public class RoleDto
    {
        public byte Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên vai trò.")]
        [StringLength(50, ErrorMessage = "Tên vai trò tối đa 50 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public bool IsProtected { get; set; }
    }
}