using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.Admin.CourseOffering
{
    public class CourseOfferingDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giảng viên.")]
        public int? UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học kỳ.")]
        public int? SemesterId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn môn học.")]
        [StringLength(10, ErrorMessage = "Mã môn học tối đa 10 ký tự.")]
        public string? SubjectId { get; set; }

        // Trường này chỉ phục vụ UI: lọc học kỳ theo năm học
        public int? AcademyYearId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lớp học phần.")]
        [StringLength(10, ErrorMessage = "Lớp học phần tối đa 10 ký tự.")]
        public string? ClassName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nhóm học phần.")]
        [StringLength(2, ErrorMessage = "Nhóm học phần tối đa 2 ký tự.")]
        public string? GroupNumber { get; set; }

        // display
        public string? AcademicYearName { get; set; }
        public string? UserName { get; set; }
        public string? SemesterName { get; set; }
        public string? SubjectName { get; set; }
        public int? FacultyId { get; set; }
        public string? FacultyName { get; set; }
    }
}