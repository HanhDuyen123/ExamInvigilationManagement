namespace ExamInvigilationManagement.Application.DTOs.ManualAssignment
{
    public class ManualAssignmentCurrentInvigilatorDto
    {
        public int ExamInvigilatorId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? NewUserId { get; set; }
        public string NewUserName { get; set; } = string.Empty;
        public string NewFullName { get; set; } = string.Empty;
        public byte PositionNo { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ResponseStatus { get; set; } = string.Empty;
        public string ResponseNote { get; set; } = string.Empty;
        public DateTime? ResponseAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string EffectiveFullName => string.IsNullOrWhiteSpace(NewFullName) ? FullName : NewFullName;
        public bool HasReplacement => NewUserId.HasValue;
    }
}
