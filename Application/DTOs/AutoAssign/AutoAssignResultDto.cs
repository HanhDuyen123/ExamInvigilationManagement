namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public int TotalSchedules { get; set; }
        public int AssignedInvigilators { get; set; }
        public int FullyAssignedSchedules { get; set; }
        public int MissingSchedules { get; set; }

        public List<string> Warnings { get; set; } = new();
        public List<AutoAssignScheduleResultDto> Details { get; set; } = new();
    }

    public class AutoAssignScheduleResultDto
    {
        public int ExamScheduleId { get; set; }
        public DateTime ExamDate { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public string RoomDisplay { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public string StatusBefore { get; set; } = string.Empty;
        public string StatusAfter { get; set; } = string.Empty;

        public int RequiredCount { get; set; } = 2;
        public int AssignedCount { get; set; }

        public string Message { get; set; } = string.Empty;
        public List<AutoAssignAssignedLecturerDto> AssignedLecturers { get; set; } = new();
    }

    public class AutoAssignAssignedLecturerDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public byte PositionNo { get; set; }
        public int Score { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}