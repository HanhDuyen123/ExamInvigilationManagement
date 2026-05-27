using ExamInvigilationManagement.Common.Constants;

namespace ExamInvigilationManagement.Common.Workflow
{
    public static class InvigilatorWorkflowGuard
    {
        private static readonly Dictionary<string, string[]> ResponseTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [InvigilatorResponseStatuses.Pending] = [InvigilatorResponseStatuses.Confirmed, InvigilatorResponseStatuses.Rejected],
            [InvigilatorResponseStatuses.Confirmed] = [InvigilatorResponseStatuses.Rejected],
            [InvigilatorResponseStatuses.Rejected] = [InvigilatorResponseStatuses.Confirmed]
        };

        private static readonly Dictionary<string, string[]> SubstitutionTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [InvigilatorSubstitutionStatuses.Proposed] = [InvigilatorSubstitutionStatuses.Approved, InvigilatorSubstitutionStatuses.Rejected]
        };

        public static void EnsureResponseStatusChange(string? fromStatus, string toStatus, string subject)
        {
            var from = NormalizeResponseStatus(fromStatus);
            var to = NormalizeResponseStatus(toStatus);

            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase)) return;

            if (!ResponseTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{subject} không thể chuyển phản hồi từ '{fromStatus}' sang '{toStatus}'.");
        }

        public static void EnsureSubstitutionStatusChange(string? fromStatus, string toStatus, string subject)
        {
            var from = NormalizeSubstitutionStatus(fromStatus);
            var to = NormalizeSubstitutionStatus(toStatus);

            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase)) return;

            if (!SubstitutionTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{subject} không thể chuyển đề xuất thay thế từ '{fromStatus}' sang '{toStatus}'.");
        }

        public static bool IsFinalSubstitutionStatus(string? status)
        {
            var normalized = NormalizeSubstitutionStatus(status);
            return string.Equals(normalized, InvigilatorSubstitutionStatuses.Approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, InvigilatorSubstitutionStatuses.Rejected, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeResponseStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return InvigilatorResponseStatuses.Pending;

            return InvigilatorResponseStatuses.ToDisplay(status);
        }

        private static string NormalizeSubstitutionStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return InvigilatorSubstitutionStatuses.Proposed;

            return InvigilatorSubstitutionStatuses.ToDisplay(status);
        }
    }
}
