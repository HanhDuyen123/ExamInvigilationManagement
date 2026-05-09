namespace ExamInvigilationManagement.Common.Helpers
{
    public static class NotificationTypeHelper
    {
        public const string ExamScheduleApprovalDecision = "ExamScheduleApproval";
        public const string ManualAssignmentChanged = "ManualAssignment";
        public const string InvigilatorResponse = "InvigilatorResponse";
        public const string InvigilatorSubstitution = "InvigilatorSubstitution";
        public const string SchedulePublished = "SchedulePublished";
        public const string Generic = "System";

        private const string LegacyExamScheduleApprovalDecision = "ExamScheduleApprovalDecision";
        private const string LegacyManualAssignmentChanged = "ManualAssignmentChanged";

        public static string GetLabel(string? type) => type switch
        {
            ExamScheduleApprovalDecision => "Duyệt lịch thi",
            LegacyExamScheduleApprovalDecision => "Duyệt lịch thi",
            ManualAssignmentChanged => "Phân công giám thị",
            LegacyManualAssignmentChanged => "Phân công giám thị",
            InvigilatorResponse => "Phản hồi giám thị",
            InvigilatorSubstitution => "Đề xuất thay thế",
            SchedulePublished => "Gửi lịch thi",
            Generic => "Thông báo chung",
            _ => "Thông báo"
        };

        public static string GetIcon(string? type) => type switch
        {
            ExamScheduleApprovalDecision => "bi-check2-circle",
            LegacyExamScheduleApprovalDecision => "bi-check2-circle",
            ManualAssignmentChanged => "bi-person-plus",
            LegacyManualAssignmentChanged => "bi-person-plus",
            InvigilatorResponse => "bi-chat-square-text",
            InvigilatorSubstitution => "bi-arrow-left-right",
            SchedulePublished => "bi-send",
            _ => "bi-bell"
        };
    }
}
