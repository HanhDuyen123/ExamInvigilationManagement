using ExamInvigilationManagement.Domain.Enums;

namespace ExamInvigilationManagement.Common.Helpers
{
    public static class SemesterHelper
    {
        public static string ToName(SemesterType type)
        {
            return type switch
            {
                SemesterType.Semester1 => "Học kỳ 1",
                SemesterType.Semester2 => "Học kỳ 2",
                SemesterType.Summer => "Hè",
                _ => ""
            };
        }

        public static SemesterType? ToType(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            name = name.Trim().ToLower();

            return name switch
            {
                "1" or "hk1" or "học kỳ 1" => SemesterType.Semester1,
                "2" or "hk2" or "học kỳ 2" => SemesterType.Semester2,
                "hè" or "hk hè" or "summer" => SemesterType.Summer,
                _ => null
            };
        }

        public static string ToDisplayName(string name)
        {
            return ToType(name) switch
            {
                SemesterType.Semester1 => "Học kỳ 1",
                SemesterType.Semester2 => "Học kỳ 2",
                SemesterType.Summer => "Hè",
                _ => name
            };
        }
    }
}
