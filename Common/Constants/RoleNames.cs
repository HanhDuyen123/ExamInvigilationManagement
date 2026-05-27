namespace ExamInvigilationManagement.Common.Constants
{
    public static class RoleNames
    {
        public const string AdminCode = "Admin";
        public const string LecturerCode = "Lecturer";
        public const string SecretaryCode = "Secretary";
        public const string DeanCode = "Dean";

        public const string Admin = "Admin";
        public const string Lecturer = "Giảng viên";
        public const string Secretary = "Thư ký khoa";
        public const string Dean = "Trưởng khoa";

        public static string ToCode(string? role)
        {
            return (role ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "admin" => AdminCode,
                "lecturer" or "giảng viên" => LecturerCode,
                "secretary" or "thư ký khoa" => SecretaryCode,
                "dean" or "trưởng khoa" => DeanCode,
                _ => role ?? string.Empty
            };
        }

        public static string ToDisplay(string? codeOrDisplay)
        {
            return ToCode(codeOrDisplay) switch
            {
                AdminCode => Admin,
                LecturerCode => Lecturer,
                SecretaryCode => Secretary,
                DeanCode => Dean,
                _ => codeOrDisplay ?? string.Empty
            };
        }
    }
}
