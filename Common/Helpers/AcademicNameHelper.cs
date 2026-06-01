namespace ExamInvigilationManagement.Common.Helpers
{
    public static class AcademicNameHelper
    {
        public static string NormalizePeriodName(string? name)
        {
            var value = NormalizeKey(name);
            return value switch
            {
                "1" or "dot1" or "dot 1" or "lan1" or "lan 1" => "Đợt 1",
                "2" or "dot2" or "dot 2" or "lan2" or "lan 2" => "Đợt 2",
                "giua ky" or "giuaky" or "giua ki" or "giuaki" => "Giữa kỳ",
                "cuoi ky" or "cuoiky" or "cuoi ki" or "cuoiki" => "Cuối kỳ",
                _ => (name ?? string.Empty).Trim()
            };
        }

        public static string NormalizeSessionName(string? name)
        {
            var value = NormalizeKey(name);
            return value switch
            {
                "sang" or "buoi sang" => "Sáng",
                "chieu" or "buoi chieu" => "Chiều",
                "toi" or "buoi toi" => "Tối",
                _ => (name ?? string.Empty).Trim()
            };
        }

        public static string NormalizeSlotName(string? name)
        {
            var value = NormalizeKey(name);
            return value switch
            {
                "1" or "ca1" or "ca 1" => "Ca 1",
                "2" or "ca2" or "ca 2" => "Ca 2",
                "3" or "ca3" or "ca 3" => "Ca 3",
                _ => (name ?? string.Empty).Trim()
            };
        }

        private static string NormalizeKey(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace("đ", "d")
                .Replace("ợ", "o")
                .Replace("ơ", "o")
                .Replace("ớ", "o")
                .Replace("ờ", "o")
                .Replace("ở", "o")
                .Replace("ỡ", "o")
                .Replace("ộ", "o")
                .Replace("ô", "o")
                .Replace("ố", "o")
                .Replace("ồ", "o")
                .Replace("ổ", "o")
                .Replace("ỗ", "o")
                .Replace("ọ", "o")
                .Replace("ó", "o")
                .Replace("ò", "o")
                .Replace("ỏ", "o")
                .Replace("õ", "o")
                .Replace("ậ", "a")
                .Replace("ă", "a")
                .Replace("ắ", "a")
                .Replace("ằ", "a")
                .Replace("ẳ", "a")
                .Replace("ẵ", "a")
                .Replace("ặ", "a")
                .Replace("â", "a")
                .Replace("ấ", "a")
                .Replace("ầ", "a")
                .Replace("ẩ", "a")
                .Replace("ẫ", "a")
                .Replace("ạ", "a")
                .Replace("á", "a")
                .Replace("à", "a")
                .Replace("ả", "a")
                .Replace("ã", "a")
                .Replace("ệ", "e")
                .Replace("ê", "e")
                .Replace("ế", "e")
                .Replace("ề", "e")
                .Replace("ể", "e")
                .Replace("ễ", "e")
                .Replace("ẹ", "e")
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ẻ", "e")
                .Replace("ẽ", "e")
                .Replace("ị", "i")
                .Replace("í", "i")
                .Replace("ì", "i")
                .Replace("ỉ", "i")
                .Replace("ĩ", "i")
                .Replace("ự", "u")
                .Replace("ư", "u")
                .Replace("ứ", "u")
                .Replace("ừ", "u")
                .Replace("ử", "u")
                .Replace("ữ", "u")
                .Replace("ụ", "u")
                .Replace("ú", "u")
                .Replace("ù", "u")
                .Replace("ủ", "u")
                .Replace("ũ", "u")
                .Replace("ỵ", "y")
                .Replace("ý", "y")
                .Replace("ỳ", "y")
                .Replace("ỷ", "y")
                .Replace("ỹ", "y");
        }
    }
}
