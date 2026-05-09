namespace ExamInvigilationManagement.Domain.Entities
{
    public class ExamInvigilator
    {
        public int Id { get; set; }

        public int AssigneeId { get; set; }
        public int AssignerId { get; set; }
        public int? NewAssigneeId { get; set; }

        public int ExamScheduleId { get; set; }
        public byte PositionNo { get; set; }

        public string Status { get; set; } = null!;

        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        public User? Assignee { get; set; }
        public User? Assigner { get; set; }
    }
}
