using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OnlineGame.Game.GameObjects.Player
{
    /// <summary>
    /// Represents the player's profile, containing their username and hashed password.
    /// </summary>
    public class PlayerProfile
    {
        // Username of the player, also used in the file name
        public string Username { get; set; } = string.Empty;

        // Privately stores the hashed password
        private string _hashedPassword = string.Empty;

        /// <summary>
        /// Gets the hashed password for the player.
        /// </summary>
        public string HashedPassword => _hashedPassword;

        private static readonly JsonSerializerOptions CachedSerialierOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Sets a plaintext password and securely hashes it for storage.
        /// </summary>
        /// <param name="password">The plaintext password to hash and store.</param>
        public void SetPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            _hashedPassword = HashPassword(password);
        }

        /// <summary>
        /// Verifies a plaintext password against the stored hash.
        /// </summary>
        /// <param name="password">The plaintext password to verify.</param>
        /// <returns>True if the password matches the stored hash; otherwise, false.</returns>
        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            return HashPassword(password) == _hashedPassword;
        }

        /// <summary>
        /// Serializes the PlayerProfile to JSON format.
        /// Only the username and hashed password are included.
        /// </summary>
        /// <returns>A JSON string representation of the PlayerProfile.</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(new
            {
                Username,
                PasswordHash = _hashedPassword
            }, CachedSerialierOptions);
        }

        /// <summary>
        /// Deserializes a JSON string into a PlayerProfile instance.
        /// </summary>
        /// <param name="json">The JSON string containing the player data.</param>
        /// <returns>A PlayerProfile instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the JSON is invalid or does not contain required keys.</exception>
        public static PlayerProfile FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? 
                throw new InvalidOperationException("Invalid JSON format for PlayerProfile.");

            // Use TryGetValue to retrieve values and validate presence
            if (!data.TryGetValue("Username", out var username) || string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("Invalid or missing 'Username' in JSON.");

            if (!data.TryGetValue("PasswordHash", out var passwordHash) || string.IsNullOrWhiteSpace(passwordHash))
                throw new InvalidOperationException("Invalid or missing 'PasswordHash' in JSON.");

            return new PlayerProfile
            {
                Username = username,
                _hashedPassword = passwordHash
            };
        }


        /// <summary>
        /// Hashes a plaintext password using SHA256.
        /// </summary>
        /// <param name="password">The plaintext password to hash.</param>
        /// <returns>The hashed password as a Base64 string.</returns>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
