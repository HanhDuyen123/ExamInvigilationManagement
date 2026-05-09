namespace ExamInvigilationManagement.Application.DTOs.ManualAssignment
{
    public class ManualAssignmentLecturerOptionDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public int? FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;

        public int CurrentLoad { get; set; }
        public int SameDayLoad { get; set; }

        public bool IsExactOwner { get; set; }
        public bool HasTaughtSubject { get; set; }
        public bool HasTaughtClass { get; set; }
        public int PeriodLoad { get; set; }
        public int PriorityScore { get; set; }
        public bool CanSelect { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RecommendationLabel { get; set; } = string.Empty;
        public string WorkloadLabel { get; set; } = string.Empty;
        public string AvailabilityLabel { get; set; } = string.Empty;
    }
}
