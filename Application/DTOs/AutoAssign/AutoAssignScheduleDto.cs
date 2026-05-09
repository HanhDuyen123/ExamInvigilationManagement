namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignScheduleDto
    {
        public int ExamScheduleId { get; set; }
        public int SlotId { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public TimeOnly TimeStart { get; set; }

        public int AcademyYearId { get; set; }
        public int SemesterId { get; set; }
        public int PeriodId { get; set; }
        public int SessionId { get; set; }

        public int RoomId { get; set; }
        public string RoomDisplay { get; set; } = string.Empty;

        public int OfferingId { get; set; }
        public int OfferingUserId { get; set; }
        public int? OfferingFacultyId { get; set; }

        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GroupNumber { get; set; } = string.Empty;

        public DateTime ExamDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}