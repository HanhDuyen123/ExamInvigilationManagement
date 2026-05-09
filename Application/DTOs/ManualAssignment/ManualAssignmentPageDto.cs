namespace ExamInvigilationManagement.Application.DTOs.ManualAssignment
{
    public class ManualAssignmentPageDto
    {
        public ManualAssignmentScheduleDto Schedule { get; set; } = null!;
        public List<ManualAssignmentCurrentInvigilatorDto> CurrentInvigilators { get; set; } = new();
        public List<ManualAssignmentLecturerOptionDto> LecturerOptions { get; set; } = new();
        public List<ManualAssignmentActivityLogDto> ActivityLogs { get; set; } = new();

        public ManualAssignmentRequestDto Request { get; set; } = new();

        public List<byte> MissingPositions { get; set; } = new();
        public ExamInvigilationManagement.Application.DTOs.InvigilatorSubstitution.InvigilatorSubstitutionDetailDto? SubstitutionReview { get; set; }
    }
}
