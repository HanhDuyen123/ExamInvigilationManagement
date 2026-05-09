namespace ExamInvigilationManagement.Domain.Entities
{
    public class ExamSchedule
    {
        public int Id { get; set; }

        public int SlotId { get; set; }
        public int AcademyYearId { get; set; }
        public int SemesterId { get; set; }
        public int PeriodId { get; set; }
        public int SessionId { get; set; }
        public int RoomId { get; set; }
        public int OfferingId { get; set; }

        public DateTime ExamDate { get; set; }
        public string Status { get; set; } = null!;

        public ExamSlot? Slot { get; set; }
        public Room? Room { get; set; }
        public CourseOffering? Offering { get; set; }
    }
}
