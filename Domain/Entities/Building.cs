using ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Domain.Entities
{
    public class Building
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;

        public List<Room> Rooms { get; set; } = new();
    }
}
