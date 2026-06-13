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
        public bool IsPreview { get; set; }
        public bool IsOptimizationProven { get; set; } = true;
        public bool HasSavedDraft { get; set; }
        public bool DraftSaved { get; set; }
        public bool DraftCleared { get; set; }
        public int? AssignerId { get; set; }
        public int? SemesterId { get; set; }
        public int? PeriodId { get; set; }
        public string? PreviewToken { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public AutoAssignPlanDto? PlanSnapshot { get; set; }

        public List<string> Warnings { get; set; } = new();
        public AutoAssignComparisonDto? Comparison { get; set; }
        public List<AutoAssignScheduleResultDto> Details { get; set; } = new();
    }

    public class AutoAssignComparisonDto
    {
        public bool HasBaseline { get; set; }
        public int BaselineAssignedInvigilators { get; set; }
        public int BaselineFullyAssignedSchedules { get; set; }
        public int BaselineMissingSchedules { get; set; }
        public int ChangedSchedules { get; set; }
        public int AddedAssignments { get; set; }
        public int RemovedAssignments { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<AutoAssignScheduleComparisonDto> ChangedDetails { get; set; } = new();
    }

    public class AutoAssignScheduleComparisonDto
    {
        public int ExamScheduleId { get; set; }
        public DateTime ExamDate { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public string RoomDisplay { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string StatusBefore { get; set; } = string.Empty;
        public string StatusAfter { get; set; } = string.Empty;
        public List<AutoAssignPositionComparisonDto> Positions { get; set; } = new();
    }

    public class AutoAssignPositionComparisonDto
    {
        public byte PositionNo { get; set; }
        public string BaselineLecturerName { get; set; } = string.Empty;
        public string CurrentLecturerName { get; set; } = string.Empty;
        public bool Changed { get; set; }
    }

    public class AutoAssignScheduleResultDto
    {
        public int ExamScheduleId { get; set; }
        public DateTime ExamDate { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public string RoomDisplay { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string ExamFormatDisplay { get; set; } = string.Empty;

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
