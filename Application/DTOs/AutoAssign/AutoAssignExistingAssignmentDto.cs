namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignExistingAssignmentDto
    {
        public int ExamInvigilatorId { get; set; }
        public int ExamScheduleId { get; set; }
        public int UserId { get; set; }
        public byte PositionNo { get; set; }
        public int SlotId { get; set; }
        public DateTime ExamDate { get; set; }
        public string InvigilatorStatus { get; set; } = string.Empty;
        public string ResponseStatus { get; set; } = string.Empty;

        public bool IsRejected =>
            InvigilatorStatus.Equals("Từ chối", StringComparison.OrdinalIgnoreCase) ||
            ResponseStatus.Equals("Từ chối", StringComparison.OrdinalIgnoreCase);
    }
}
