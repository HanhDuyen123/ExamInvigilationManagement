namespace ExamInvigilationManagement.Common.Constants
{
    public static class ExamScheduleStatuses
    {
        public const string WaitingAssign = "Chờ phân công";
        public const string MissingInvigilator = "Thiếu giám thị";
        public const string PendingApproval = "Chờ duyệt";
        public const string Approved = "Đã duyệt";
        public const string ApprovalRejected = "Từ chối duyệt";
    }

    public static class ExamInvigilatorStatuses
    {
        public const string PendingConfirmation = "Chờ xác nhận";
        public const string Confirmed = "Xác nhận";
        public const string Rejected = "Từ chối";
    }

    public static class InvigilatorResponseStatuses
    {
        public const string Pending = "Chưa phản hồi";
        public const string Confirmed = "Xác nhận";
        public const string Rejected = "Từ chối";
    }

    public static class InvigilatorSubstitutionStatuses
    {
        public const string Proposed = "Đã đề xuất";
        public const string Approved = "Đã duyệt";
        public const string Rejected = "Từ chối duyệt";
    }
}
