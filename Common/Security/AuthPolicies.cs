namespace ExamInvigilationManagement.Common.Security
{
    public static class AuthPolicies
    {
        public const string CanManageSystem = nameof(CanManageSystem);
        public const string CanManageExams = nameof(CanManageExams);
        public const string CanAssignInvigilators = nameof(CanAssignInvigilators);
        public const string CanApproveSchedule = nameof(CanApproveSchedule);
        public const string CanViewSensitiveData = nameof(CanViewSensitiveData);
    }
}
