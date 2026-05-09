namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignPlanDto
    {
        public List<AutoAssignInvigilatorCreateDto> NewInvigilators { get; set; } = new();
        public List<AutoAssignScheduleStatusUpdateDto> ScheduleStatuses { get; set; } = new();
    }

    public class AutoAssignInvigilatorCreateDto
    {
        public int AssigneeId { get; set; }
        public int AssignerId { get; set; }
        public int? NewAssigneeId { get; set; }
        public int ExamScheduleId { get; set; }
        public byte PositionNo { get; set; }
        public string Status { get; set; } = "chờ";
        public DateTime? CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; }
    }

    public class AutoAssignScheduleStatusUpdateDto
    {
        public int ExamScheduleId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}