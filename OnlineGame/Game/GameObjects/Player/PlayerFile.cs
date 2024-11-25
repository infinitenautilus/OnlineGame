using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OnlineGame.Utility;

namespace OnlineGame.Game.GameObjects.Player
{
    /// <summary>
    /// Represents the player's profile, containing their username and hashed password.
    /// </summary>
    public class PlayerFile
    {
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public static async Task SavePlayerAsync(PlayerFile player)
        {
            string filePath = $@"{Constellations.PLAYERSTORAGE}{player.UserName.ToLower()}.json";
            string jsonData = System.Text.Json.JsonSerializer.Serialize(player);
            await File.WriteAllTextAsync(filePath, jsonData);
        }

        public static async Task<PlayerFile?> LoadPlayerAsync(string userName)
        {
            string filePath = $@"{Constellations.PLAYERSTORAGE}{userName.ToLower()}.json";

            if (!File.Exists(filePath))
                return null;

            string jsonData = await File.ReadAllTextAsync(filePath);
            return System.Text.Json.JsonSerializer.Deserialize<PlayerFile>(jsonData);
        }
    }

}
