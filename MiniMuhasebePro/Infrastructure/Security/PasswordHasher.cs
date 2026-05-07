using System;
using System.Security.Cryptography;
using System.Text;

namespace MiniMuhasebePro.Infrastructure.Security
{
    public static class PasswordHasher
    {
        public static string Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }

        public static bool Verify(string plain, string hash)
        {
            return Hash(plain) == hash;
        }
    }
}
