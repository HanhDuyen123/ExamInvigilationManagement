using ExamInvigilationManagement.Common.Constants;

namespace ExamInvigilationManagement.Common.Helpers
{
    public static class ExamScheduleStatusHelper
    {
        public const string WaitingAssign = ExamScheduleStatuses.WaitingAssign;
        public const string MissingInvigilator = ExamScheduleStatuses.MissingInvigilator;
        public const string Pending = ExamScheduleStatuses.PendingApproval;
        public const string Approved = ExamScheduleStatuses.Approved;
        public const string Rejected = ExamScheduleStatuses.ApprovalRejected;

        private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            WaitingAssign,
            MissingInvigilator,
            Pending,
            Approved,
            Rejected
        };

        public static string Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Pending;

            return status.Trim().ToLowerInvariant() switch
            {
                "waitingassign" => WaitingAssign,
                "chờ phân công" => WaitingAssign,
                "missinginvigilator" => MissingInvigilator,
                "thiếu giám thị" => MissingInvigilator,
                "pending" => Pending,
                "chờ duyệt" => Pending,
                "approved" => Approved,
                "đã duyệt" => Approved,
                "rejected" => Rejected,
                "từ chối duyệt" => Rejected,
                _ => status.Trim()
            };
        }

        public static bool IsValid(string? status)
            => Allowed.Contains(Normalize(status));

        public static string ToDisplay(string? status)
            => Normalize(status);
    }
}
