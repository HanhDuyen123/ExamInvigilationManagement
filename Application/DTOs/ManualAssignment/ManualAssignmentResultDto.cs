namespace ExamInvigilationManagement.Application.DTOs.ManualAssignment
{
    public class ManualAssignmentResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public int ExamScheduleId { get; set; }
        public int AssignedCount { get; set; }
        public string StatusAfter { get; set; } = string.Empty;

        public List<string> Errors { get; set; } = new();
    }
}