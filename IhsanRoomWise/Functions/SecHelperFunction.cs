// Functions\SecHelperFunction.cs

using System;
using System.Security.Cryptography;
using System.Text;

namespace IhsanRoomWise.Functions
{
    public static class SecHelperFunction
    {
        public static string HashPasswordMD5(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string GenerateResetToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            byte[] randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            char[] result = new char[32];
            for (int i = 0; i < 32; i++)
            {
                result[i] = chars[randomBytes[i] % chars.Length];
            }
            return new string(result);
        }

        public static bool VerifyPassword(string providedPasswordHash, string storedPasswordHash)
        {
            return string.Equals(providedPasswordHash, storedPasswordHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}