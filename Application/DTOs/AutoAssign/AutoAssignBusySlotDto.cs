namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignBusySlotDto
    {
        public int UserId { get; set; }
        public int SlotId { get; set; }
        public DateOnly BusyDate { get; set; }
    }
}