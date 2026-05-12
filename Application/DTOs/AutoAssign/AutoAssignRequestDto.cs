using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignRequestDto
    {
        [Required(ErrorMessage = "Vui lòng chọn học kỳ.")]
        public int? SemesterId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đợt thi.")]
        public int? PeriodId { get; set; }

        [Required(ErrorMessage = "Không xác định được người thực hiện.")]
        public int AssignerId { get; set; }
    }
}
