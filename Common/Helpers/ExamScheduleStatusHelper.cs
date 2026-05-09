namespace ExamInvigilationManagement.Common.Helpers
{
    public static class ExamScheduleStatusHelper
    {
        public const string WaitingAssign = "Chờ phân công";
        public const string MissingInvigilator = "Thiếu giám thị";
        public const string Pending = "Chờ duyệt";
        public const string Approved = "Đã duyệt";
        public const string Rejected = "Từ chối duyệt";

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