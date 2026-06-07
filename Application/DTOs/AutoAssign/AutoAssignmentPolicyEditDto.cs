using System.ComponentModel.DataAnnotations;

namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignmentPolicyEditDto
    {
        public int? PolicyId { get; set; }
        public int FacultyId { get; set; }

        [Required(ErrorMessage = "Tên chính sách là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên chính sách không được vượt quá 100 ký tự.")]
        public string PolicyName { get; set; } = "Chính sách phân công mặc định";

        [Range(1, 10, ErrorMessage = "Số giám thị mỗi lịch phải từ 1 đến 10.")]
        public int RequiredInvigilatorsPerSchedule { get; set; } = 2;

        public bool AllowCrossFaculty { get; set; }

        public bool RequirePeriodAvailabilityIfExists { get; set; } = true;

        public bool AllowFacultyMemberAsFallback { get; set; } = true;

        [Range(1, 5, ErrorMessage = "Số ca tối đa trong ngày phải từ 1 đến 5.")]
        public int? MaxAssignmentsPerDay { get; set; }

        [Range(1, 200, ErrorMessage = "Số ca tối đa trong đợt phải từ 1 đến 200.")]
        public int? MaxAssignmentsPerPeriod { get; set; }

        [Range(1, 3, ErrorMessage = "Số lịch trùng slot tối đa phải từ 1 đến 3.")]
        public int MaxAssignmentsPerSlot { get; set; } = 1;

        [Range(1, 60, ErrorMessage = "Thời gian solver phải từ 1 đến 60 giây.")]
        public int SolverTimeLimitSeconds { get; set; } = 8;

        public bool IsDatabasePolicy { get; set; }
        public List<AutoAssignmentRuleEditDto> Rules { get; set; } = new();
        public List<AutoAssignmentExamFormatPolicyEditDto> ExamFormatPolicies { get; set; } = new();
    }

    public class AutoAssignmentRuleEditDto
    {
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool IsRequired { get; set; }
        public int PriorityOrder { get; set; }

        [Range(-100000, 100000, ErrorMessage = "Trọng số phải nằm trong giới hạn an toàn.")]
        public int Weight { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class AutoAssignmentExamFormatPolicyEditDto
    {
        public int ExamFormatId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AssignmentMode { get; set; } = AutoAssignmentExamFormatAssignmentModes.Full;
    }
}
