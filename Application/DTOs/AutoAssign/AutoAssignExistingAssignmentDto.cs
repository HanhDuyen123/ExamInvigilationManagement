namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignExistingAssignmentDto
    {
        public int ExamScheduleId { get; set; }
        public int UserId { get; set; }
        public int SlotId { get; set; }
        public DateTime ExamDate { get; set; }
    }
}