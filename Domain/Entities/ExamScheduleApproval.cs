namespace ExamInvigilationManagement.Domain.Entities
{
    public class ExamScheduleApproval
    {
        public int Id { get; set; }

        public int ExamScheduleId { get; set; }
        public int ApproverId { get; set; }

        public string Status { get; set; } = null!;
        public string? Note { get; set; }

        public DateTime? ApproveAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
