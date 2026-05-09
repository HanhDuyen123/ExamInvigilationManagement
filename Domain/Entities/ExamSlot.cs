using ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Domain.Entities
{
    public class ExamSlot
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Name { get; set; } = null!;

        public TimeOnly TimeStart { get; set; }

        public ExamSession Session { get; set; } = null!;

        public List<ExamSchedule> ExamSchedules { get; set; } = new();
        public List<LecturerBusySlot> LecturerBusySlots { get; set; } = new();
    }
}
