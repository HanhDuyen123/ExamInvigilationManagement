using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.ManualAssignment
{
    public class ManualAssignmentRequestDto
    {
        [Required]
        public int ExamScheduleId { get; set; }

        [Required]
        public int AssignerId { get; set; }

        public int? Position1AssigneeId { get; set; }
        public int? Position2AssigneeId { get; set; }
    }
}
