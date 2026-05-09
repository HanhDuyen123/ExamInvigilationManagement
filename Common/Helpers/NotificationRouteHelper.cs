namespace ExamInvigilationManagement.Common.Helpers
{
    public static class NotificationRouteHelper
    {
        public static string ResolveUrl(string? type, int? relatedId)
        {
            return type switch
            {
                NotificationTypeHelper.ExamScheduleApprovalDecision =>
                    relatedId.HasValue
                        ? $"/ExamSchedule/Details/{relatedId.Value}"
                        : "/ExamSchedule?status=" + Uri.EscapeDataString("Đã duyệt"),

                "ExamScheduleApprovalDecision" =>
                    relatedId.HasValue
                        ? $"/ExamSchedule/Details/{relatedId.Value}"
                        : "/ExamSchedule?status=" + Uri.EscapeDataString("Đã duyệt"),

                NotificationTypeHelper.ManualAssignmentChanged =>
                    relatedId.HasValue
                        ? $"/Secretary/ManualAssignment/Assign?scheduleId={relatedId.Value}"
                        : "/Secretary/ManualAssignment",

                NotificationTypeHelper.InvigilatorResponse =>
                    relatedId.HasValue
                        ? $"/Secretary/ManualAssignment/Assign?scheduleId={relatedId.Value}"
                        : "/ExamSchedule",

                NotificationTypeHelper.InvigilatorSubstitution =>
                    relatedId.HasValue
                        ? $"/Secretary/ManualAssignment/Assign?substitutionId={relatedId.Value}"
                        : "/Secretary/InvigilatorSubstitution",

                NotificationTypeHelper.SchedulePublished =>
                    "/Lecturer/InvigilatorResponse?status=" + Uri.EscapeDataString("Chưa phản hồi"),

                _ => "/Notification"
            };
        }
    }
}
