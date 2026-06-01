namespace ExamInvigilationManagement.Common.Helpers
{
    public static class NotificationRouteHelper
    {
        public static string ResolveUrl(string? type, int? relatedId, string? title = null, string? content = null)
        {
            var text = $"{title} {content}";
            return type switch
            {
                NotificationTypeHelper.ExamScheduleApprovalDecision =>
                    ResolveApprovalUrl(text, relatedId),

                "ExamScheduleApprovalDecision" =>
                    ResolveApprovalUrl(text, relatedId),

                NotificationTypeHelper.ManualAssignmentChanged =>
                    relatedId.HasValue
                        ? $"/Secretary/ManualAssignment/Assign?scheduleId={relatedId.Value}"
                        : "/Secretary/ManualAssignment",

                NotificationTypeHelper.InvigilatorResponse =>
                    relatedId.HasValue
                        ? $"/Secretary/ManualAssignment/Assign?scheduleId={relatedId.Value}&focus=rejected"
                        : "/ExamSchedule?status=" + Uri.EscapeDataString("Đã duyệt"),

                NotificationTypeHelper.InvigilatorSubstitution =>
                    relatedId.HasValue
                        ? $"/Secretary/InvigilatorSubstitution/Details/{relatedId.Value}"
                        : "/Secretary/InvigilatorSubstitution",

                NotificationTypeHelper.SchedulePublished =>
                    "/Lecturer/InvigilatorResponse?status=" + Uri.EscapeDataString("Chưa phản hồi"),

                _ => "/Notification"
            };
        }

        private static string ResolveApprovalUrl(string text, int? relatedId)
        {
            if (text.Contains("từ chối", StringComparison.OrdinalIgnoreCase))
                return "/ExamSchedule?status=" + Uri.EscapeDataString("Từ chối duyệt");

            if (text.Contains("đã duyệt", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("đã được duyệt", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("duyệt thành công", StringComparison.OrdinalIgnoreCase))
            {
                return "/ExamSchedule?status=" + Uri.EscapeDataString("Đã duyệt");
            }

            if (text.Contains("gửi", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("chờ duyệt", StringComparison.OrdinalIgnoreCase))
            {
                return "/Secretary/ExamScheduleApproval?status=" + Uri.EscapeDataString("Chờ duyệt");
            }

            return relatedId.HasValue
                ? $"/ExamSchedule/Details/{relatedId.Value}"
                : "/ExamSchedule";
        }
    }
}
