namespace ExamInvigilationManagement.Domain.Entities
{
    public class LecturerBusySlot
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int SlotId { get; set; }

        public DateOnly BusyDate { get; set; }
        public string Note { get; set; } = string.Empty;

        public DateTime? CreateAt { get; set; }
        public string ApprovalStatus { get; set; } = "Chờ duyệt";
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }

        public User? User { get; set; }
        public ExamSlot? Slot { get; set; }
    }
}
