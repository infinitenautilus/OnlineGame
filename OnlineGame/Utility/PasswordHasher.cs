using System.Security.Cryptography;
using System.Text;

namespace OnlineGame.Utility
{
    public static class PasswordHasher
    {
        /// <summary>
        /// Hashes a password using SHA256.
        /// </summary>
        /// <param name="password">The plaintext password to hash.</param>
        /// <returns>The hashed password as a hexadecimal string.</returns>
        public static string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = SHA256.HashData(passwordBytes);

            // Convert to hexadecimal string
            StringBuilder hashString = new();
            foreach (byte b in hashBytes)
            {
                hashString.Append(b.ToString("x2")); // Hexadecimal format
            }

            return hashString.ToString();
        }

        /// <summary>
        /// Verifies if a plaintext password matches a stored hash.
        /// </summary>
        /// <param name="password">The plaintext password to verify.</param>
        /// <param name="storedHash">The stored hashed password.</param>
        /// <returns>True if the password matches the hash, otherwise false.</returns>
        public static bool VerifyPassword(string password, string storedHash)
        {
            string hashedPassword = HashPassword(password);
            return hashedPassword == storedHash;
        }
    }
}
