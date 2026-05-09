using ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Domain.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string BuildingId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int? Capacity { get; set; }

        public Building Building { get; set; } = null!;

        public List<ExamSchedule> ExamSchedules { get; set; } = new();
    }
}
