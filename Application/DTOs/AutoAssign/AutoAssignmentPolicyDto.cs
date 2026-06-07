namespace ExamInvigilationManagement.Application.DTOs.AutoAssign
{
    public class AutoAssignmentPolicyDto
    {
        public int? PolicyId { get; set; }
        public string PolicyName { get; set; } = "Chính sách mặc định hệ thống";
        public bool IsDatabasePolicy => PolicyId.HasValue && PolicyId.Value > 0;
        public int RequiredInvigilatorsPerSchedule { get; set; } = 2;
        public bool AllowCrossFaculty { get; set; }
        public bool RequirePeriodAvailabilityIfExists { get; set; } = true;
        public bool AllowFacultyMemberAsFallback { get; set; } = true;
        public int? MaxAssignmentsPerDay { get; set; }
        public int? MaxAssignmentsPerPeriod { get; set; }
        public int MaxAssignmentsPerSlot { get; set; } = 1;
        public int SolverTimeLimitSeconds { get; set; } = 8;
        public Dictionary<string, AutoAssignmentRuleDto> Rules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, AutoAssignmentExamFormatPolicyDto> ExamFormatPolicies { get; set; } = new();

        public static AutoAssignmentPolicyDto Default() => new()
        {
            Rules = AutoAssignmentPolicyDefaults.BuildDefaultRules()
        };

        public int GetWeight(string ruleCode, int fallback)
        {
            if (!Rules.TryGetValue(ruleCode, out var rule))
                return fallback;

            return rule.IsEnabled ? rule.Weight : 0;
        }

        public bool IsRuleEnabled(string ruleCode, bool fallback = true)
        {
            return Rules.TryGetValue(ruleCode, out var rule)
                ? rule.IsEnabled
                : fallback;
        }

        public string GetAssignmentMode(int? examFormatId)
        {
            if (!examFormatId.HasValue)
                return AutoAssignmentExamFormatAssignmentModes.Full;

            return ExamFormatPolicies.TryGetValue(examFormatId.Value, out var policy)
                ? policy.AssignmentMode
                : AutoAssignmentExamFormatAssignmentModes.Full;
        }
    }

    public class AutoAssignmentExamFormatPolicyDto
    {
        public int ExamFormatId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AssignmentMode { get; set; } = AutoAssignmentExamFormatAssignmentModes.Full;
    }

    public static class AutoAssignmentExamFormatAssignmentModes
    {
        public const string Full = "Full";
        public const string OwnerOnly = "OwnerOnly";
        public const string Skip = "Skip";
    }

    public class AutoAssignmentRuleDto
    {
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool IsRequired { get; set; }
        public int PriorityOrder { get; set; } = 100;
        public int Weight { get; set; }
        public string? ParametersJson { get; set; }
    }

    public static class AutoAssignmentPolicyRuleCodes
    {
        public const string ExactOwner = "EXACT_OWNER";
        public const string SameSubject = "SAME_SUBJECT";
        public const string Emergency = "EMERGENCY";
        public const string FacultyMember = "FACULTY_MEMBER";
        public const string LowLoad = "LOW_LOAD";
        public const string SameDayLoad = "SAME_DAY_LOAD";
        public const string Location = "LOCATION";
        public const string OwnerReservePenalty = "OWNER_RESERVE_PENALTY";
        public const string OralSpecialist = "ORAL_SPECIALIST";
        public const string PracticalSpecialist = "PRACTICAL_SPECIALIST";
        public const string Shortage = "SHORTAGE";
    }

    public static class AutoAssignmentPolicyDefaults
    {
        public static Dictionary<string, AutoAssignmentRuleDto> BuildDefaultRules()
        {
            return new(StringComparer.OrdinalIgnoreCase)
            {
                [AutoAssignmentPolicyRuleCodes.ExactOwner] = Rule(AutoAssignmentPolicyRuleCodes.ExactOwner, "Ưu tiên giảng viên đang dạy lớp", "Objective", 10, 12_000),
                [AutoAssignmentPolicyRuleCodes.SameSubject] = Rule(AutoAssignmentPolicyRuleCodes.SameSubject, "Ưu tiên giảng viên cùng chuyên môn môn thi", "Objective", 20, 8_000),
                [AutoAssignmentPolicyRuleCodes.Emergency] = Rule(AutoAssignmentPolicyRuleCodes.Emergency, "Ứng viên phù hợp lịch khi thiếu chuyên môn", "Soft", 80, 3_000),
                [AutoAssignmentPolicyRuleCodes.FacultyMember] = Rule(AutoAssignmentPolicyRuleCodes.FacultyMember, "Dự phòng từ vai trò trong khoa", "Soft", 90, 8_000),
                [AutoAssignmentPolicyRuleCodes.LowLoad] = Rule(AutoAssignmentPolicyRuleCodes.LowLoad, "Ưu tiên người ít tải trong học kỳ", "Soft", 40, 700),
                [AutoAssignmentPolicyRuleCodes.SameDayLoad] = Rule(AutoAssignmentPolicyRuleCodes.SameDayLoad, "Hạn chế nhiều ca trong cùng ngày", "Soft", 50, 600),
                [AutoAssignmentPolicyRuleCodes.Location] = Rule(AutoAssignmentPolicyRuleCodes.Location, "Ưu tiên vị trí phòng thi gần nhau", "Soft", 60, 45),
                [AutoAssignmentPolicyRuleCodes.OwnerReservePenalty] = Rule(AutoAssignmentPolicyRuleCodes.OwnerReservePenalty, "Hạn chế lấy owner của lịch khác", "Soft", 70, 150),
                [AutoAssignmentPolicyRuleCodes.OralSpecialist] = Rule(AutoAssignmentPolicyRuleCodes.OralSpecialist, "Ưu tiên chuyên môn cho thi vấn đáp", "Objective", 11, 11_000),
                [AutoAssignmentPolicyRuleCodes.PracticalSpecialist] = Rule(AutoAssignmentPolicyRuleCodes.PracticalSpecialist, "Ưu tiên chuyên môn cho thi thực hành", "Objective", 12, 9_000),
                [AutoAssignmentPolicyRuleCodes.Shortage] = Rule(AutoAssignmentPolicyRuleCodes.Shortage, "Tối thiểu số lịch thiếu giám thị", "Objective", 1, 1, isRequired: true)
            };
        }

        private static AutoAssignmentRuleDto Rule(string code, string name, string type, int order, int weight, bool isRequired = false)
        {
            return new AutoAssignmentRuleDto
            {
                RuleCode = code,
                RuleName = name,
                RuleType = type,
                PriorityOrder = order,
                Weight = weight,
                IsEnabled = true,
                IsRequired = isRequired
            };
        }
    }
}
