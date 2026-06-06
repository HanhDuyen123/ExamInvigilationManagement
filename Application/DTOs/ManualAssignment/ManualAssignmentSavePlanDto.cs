namespace ExamInvigilationManagement.Application.DTOs.ManualAssignment
{
    public class ManualAssignmentSavePlanDto
    {
        public int ExamScheduleId { get; set; }
        public string StatusAfter { get; set; } = string.Empty;
        public List<ManualAssignmentInvigilatorCreateDto> NewInvigilators { get; set; } = new();
        public List<ManualAssignmentInvigilatorReplaceDto> ReplaceInvigilators { get; set; } = new();
        public List<ManualAssignmentInvigilatorRemoveDto> RemoveInvigilators { get; set; } = new();
    }

    public class ManualAssignmentInvigilatorCreateDto
    {
        public int AssigneeId { get; set; }
        public int AssignerId { get; set; }
        public int ExamScheduleId { get; set; }
        public byte PositionNo { get; set; }
        public string Status { get; set; } = "chờ";
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; } = DateTime.Now;
    }

    public class ManualAssignmentInvigilatorReplaceDto
    {
        public int ExamInvigilatorId { get; set; }
        public int NewAssigneeId { get; set; }
        public int AssignerId { get; set; }
        public DateTime UpdateAt { get; set; } = DateTime.Now;
    }

    public class ManualAssignmentInvigilatorRemoveDto
    {
        public int ExamInvigilatorId { get; set; }
        public int AssignerId { get; set; }
        public DateTime UpdateAt { get; set; } = DateTime.Now;
    }
}
