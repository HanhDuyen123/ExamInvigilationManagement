using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignRequestDto
    {
        [Required]
        public int SemesterId { get; set; }

        [Required]
        public int PeriodId { get; set; }

        [Required]
        public int AssignerId { get; set; }
    }
}