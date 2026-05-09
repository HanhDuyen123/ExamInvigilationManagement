using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.ExamSchedule
{
    public class ExamScheduleDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học phần mở.")]
        public int? OfferingId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ca thi.")]
        public int? SlotId { get; set; }

        public List<int> RoomIds { get; set; } = new();

        [Required(ErrorMessage = "Vui lòng chọn ngày thi.")]
        public DateTime? ExamDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public string? Status { get; set; }

        public string? FacultyName { get; set; }
        public int? FacultyId { get; set; }
        public string? UserName { get; set; }
        public string? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int? Credit { get; set; }
        public string? ClassName { get; set; }
        public string? GroupNumber { get; set; }

        public int? AcademyYearId { get; set; }
        public string? AcademyYearName { get; set; }

        public int? SemesterId { get; set; }
        public string? SemesterName { get; set; }

        public int? PeriodId { get; set; }
        public string? PeriodName { get; set; }

        public int? SessionId { get; set; }
        public string? SessionName { get; set; }

        public string? SlotName { get; set; }
        public TimeOnly? SlotTimeStart { get; set; }

        public int? RoomId { get; set; }
        public string? RoomName { get; set; }
        public int? RoomCapacity { get; set; }
        public string? BuildingId { get; set; }
        public string? BuildingName { get; set; }
        public int? OfferingSemesterId { get; set; }
        public int? OfferingAcademyYearId { get; set; }
        
        public string? Lecturer1Name { get; set; }
        public string? Lecturer2Name { get; set; }
        public string? Lecturer1Code { get; set; }
        public string? Lecturer2Code { get; set; }
        public string? Lecturer1FacultyName { get; set; }
        public string? Lecturer2FacultyName { get; set; }
        public int ApprovalCount { get; set; }

        // hỗ trợ UI tạo nhiều phòng
        public int RoomCount { get; set; } = 1;
    }
}
