using ExamInvigilationManagement.Common.Constants;

namespace ExamInvigilationManagement.Common.Workflow
{
    public static class StateTransitionGuard
    {
        private static readonly Dictionary<string, string[]> ExamScheduleTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [ExamScheduleStatuses.WaitingAssign] = [ExamScheduleStatuses.MissingInvigilator, ExamScheduleStatuses.PendingApproval],
            [ExamScheduleStatuses.MissingInvigilator] = [ExamScheduleStatuses.PendingApproval],
            [ExamScheduleStatuses.PendingApproval] = [ExamScheduleStatuses.Approved, ExamScheduleStatuses.ApprovalRejected],
            [ExamScheduleStatuses.ApprovalRejected] = [ExamScheduleStatuses.PendingApproval],
            [ExamScheduleStatuses.Approved] = [ExamScheduleStatuses.PendingApproval]
        };

        public static bool CanChangeExamScheduleStatus(string? fromStatus, string toStatus)
        {
            var from = NormalizeExamScheduleStatus(fromStatus);
            var to = NormalizeExamScheduleStatus(toStatus);
            return string.Equals(from, to, StringComparison.OrdinalIgnoreCase)
                   || ExamScheduleTransitions.TryGetValue(from, out var allowed)
                   && allowed.Contains(to, StringComparer.OrdinalIgnoreCase);
        }

        public static void EnsureExamScheduleStatusChange(string? fromStatus, string toStatus, string subject)
        {
            if (!CanChangeExamScheduleStatus(fromStatus, toStatus))
                throw new InvalidOperationException($"{subject} không thể chuyển trạng thái từ '{fromStatus}' sang '{toStatus}'.");
        }

        public static bool IsFinalExamScheduleStatus(string? status)
        {
            var normalized = NormalizeExamScheduleStatus(status);
            return string.Equals(normalized, ExamScheduleStatuses.Approved, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(normalized, ExamScheduleStatuses.ApprovalRejected, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeExamScheduleStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return ExamScheduleStatuses.PendingApproval;
            return ExamScheduleStatuses.ToDisplay(status);
        }
    }
}
