namespace ExamInvigilationManagement.Application.DTOs.ManualAssignment
{
    public class ManualAssignmentScheduleDto
    {
        public int ExamScheduleId { get; set; }

        public int SlotId { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public TimeOnly TimeStart { get; set; }

        public int AcademyYearId { get; set; }
        public string AcademyYearName { get; set; } = string.Empty;
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int PeriodId { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;

        public int RoomId { get; set; }
        public string RoomDisplay { get; set; } = string.Empty;

        public int OfferingId { get; set; }
        public int OfferingUserId { get; set; }
        public int OfferingUserInformationId { get; set; }
        public int OfferingUserPersonKey => OfferingUserInformationId > 0 ? OfferingUserInformationId : OfferingUserId;
        public string OfferingUserName { get; set; } = string.Empty;
        public string OfferingUserFullName { get; set; } = string.Empty;
        public int? OfferingFacultyId { get; set; }

        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GroupNumber { get; set; } = string.Empty;
        public string ExamFormatDisplay { get; set; } = string.Empty;

        public DateTime ExamDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SupportRequestedAt { get; set; }
        public bool IsSupportRequested => SupportRequestedAt.HasValue;

        public int CurrentInvigilatorCount { get; set; }
        public int MissingCount { get; set; }

        public bool CanEdit { get; set; }
        public string EditReason { get; set; } = string.Empty;
    }
}
