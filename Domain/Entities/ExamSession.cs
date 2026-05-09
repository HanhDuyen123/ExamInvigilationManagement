using ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Domain.Entities
{
    public class ExamSession
    {
        public int Id { get; set; }
        public int PeriodId { get; set; }
        public string Name { get; set; } = null!;

        public ExamPeriod Period { get; set; } = null!;

        public List<ExamSlot> ExamSlots { get; set; } = new();
        public List<ExamSchedule> ExamSchedules { get; set; } = new();
    }
}
