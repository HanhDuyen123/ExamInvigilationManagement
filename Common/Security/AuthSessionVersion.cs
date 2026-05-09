using System.Security.Cryptography;
using System.Text;

namespace ExamInvigilationManagement.Common.Security
{
    public static class AuthSessionVersion
    {
        public static string FromPasswordHash(int userId, string passwordHash)
        {
            var input = $"{userId}:{passwordHash}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            return Convert.ToBase64String(bytes);
        }
    }
}
