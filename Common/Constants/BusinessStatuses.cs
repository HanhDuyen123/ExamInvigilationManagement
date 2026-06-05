namespace ExamInvigilationManagement.Common.Constants
{
    public static class ExamScheduleStatuses
    {
        public const string WaitingAssignCode = "WaitingAssign";
        public const string MissingInvigilatorCode = "MissingInvigilator";
        public const string PendingApprovalCode = "PendingApproval";
        public const string ApprovedCode = "Approved";
        public const string ApprovalRejectedCode = "ApprovalRejected";

        public const string WaitingAssign = "Chờ phân công";
        public const string MissingInvigilator = "Thiếu giám thị";
        public const string PendingApproval = "Chờ duyệt";
        public const string Approved = "Đã duyệt";
        public const string ApprovalRejected = "Từ chối duyệt";

        public static string ToCode(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "waitingassign" or "chờ phân công" => WaitingAssignCode,
                "missinginvigilator" or "thiếu giám thị" => MissingInvigilatorCode,
                "pending" or "pendingapproval" or "chờ duyệt" => PendingApprovalCode,
                "approved" or "đã duyệt" => ApprovedCode,
                "rejected" or "approvalrejected" or "từ chối duyệt" => ApprovalRejectedCode,
                _ => status ?? string.Empty
            };
        }

        public static string ToDisplay(string? codeOrDisplay)
        {
            return ToCode(codeOrDisplay) switch
            {
                WaitingAssignCode => WaitingAssign,
                MissingInvigilatorCode => MissingInvigilator,
                PendingApprovalCode => PendingApproval,
                ApprovedCode => Approved,
                ApprovalRejectedCode => ApprovalRejected,
                _ => codeOrDisplay ?? string.Empty
            };
        }
    }

    public static class ExamInvigilatorStatuses
    {
        public const string NotSentCode = "NotSent";
        public const string PendingConfirmationCode = "PendingConfirmation";
        public const string ConfirmedCode = "Confirmed";
        public const string RejectedCode = "Rejected";

        public const string NotSent = "Chưa gửi xác nhận";
        public const string PendingConfirmation = "Chờ xác nhận";
        public const string Confirmed = "Xác nhận";
        public const string Rejected = "Từ chối";

        public static string ToCode(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "notsent" or "chưa gửi xác nhận" => NotSentCode,
                "pendingconfirmation" or "chờ xác nhận" => PendingConfirmationCode,
                "confirmed" or "xác nhận" => ConfirmedCode,
                "rejected" or "từ chối" => RejectedCode,
                _ => status ?? string.Empty
            };
        }

        public static string ToDisplay(string? codeOrDisplay)
        {
            return ToCode(codeOrDisplay) switch
            {
                NotSentCode => NotSent,
                PendingConfirmationCode => PendingConfirmation,
                ConfirmedCode => Confirmed,
                RejectedCode => Rejected,
                _ => codeOrDisplay ?? string.Empty
            };
        }
    }

    public static class InvigilatorResponseStatuses
    {
        public const string PendingCode = "Pending";
        public const string ConfirmedCode = "Confirmed";
        public const string RejectedCode = "Rejected";

        public const string Pending = "Chưa phản hồi";
        public const string Confirmed = "Xác nhận";
        public const string Rejected = "Từ chối";

        public static string ToCode(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "pending" or "chưa phản hồi" or "chờ xác nhận" or "chờ" => PendingCode,
                "confirmed" or "xác nhận" => ConfirmedCode,
                "rejected" or "từ chối" => RejectedCode,
                _ => status ?? string.Empty
            };
        }

        public static string ToDisplay(string? codeOrDisplay)
        {
            return ToCode(codeOrDisplay) switch
            {
                PendingCode => Pending,
                ConfirmedCode => Confirmed,
                RejectedCode => Rejected,
                _ => codeOrDisplay ?? string.Empty
            };
        }
    }

    public static class InvigilatorSubstitutionStatuses
    {
        public const string ProposedCode = "Proposed";
        public const string ApprovedCode = "Approved";
        public const string RejectedCode = "Rejected";

        public const string Proposed = "Đã đề xuất";
        public const string Approved = "Đã duyệt";
        public const string Rejected = "Từ chối duyệt";

        public static string ToCode(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "proposed" or "đã đề xuất" => ProposedCode,
                "approved" or "đã duyệt" => ApprovedCode,
                "rejected" or "từ chối duyệt" => RejectedCode,
                _ => status ?? string.Empty
            };
        }

        public static string ToDisplay(string? codeOrDisplay)
        {
            return ToCode(codeOrDisplay) switch
            {
                ProposedCode => Proposed,
                ApprovedCode => Approved,
                RejectedCode => Rejected,
                _ => codeOrDisplay ?? string.Empty
            };
        }
    }

    public static class BusyApprovalStatuses
    {
        public const string Pending = "Chờ duyệt";
        public const string Approved = "Đã duyệt";
        public const string Rejected = "Từ chối duyệt";

        public static bool IsFinal(string? status)
            => string.Equals(status, Approved, StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, Rejected, StringComparison.OrdinalIgnoreCase);
    }
}
