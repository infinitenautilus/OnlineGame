
// File: OnlineGame/Game/GameObjects/Player.cs
using OnlineGame.Game.Core.Types;
using OnlineGame.Game.GameObjects.ComponentInterfaces;
using OnlineGame.Game.Interfaces;
using OnlineGame.Network.Client;
using OnlineGame.Utility;
using System.IO.Pipes;
using System.Net.Sockets;

namespace OnlineGame.Game.GameObjects.Player
{
    public class PlayerObject : IPlayerObject, IHealthComponent, IMassProperties, IDescriptiveComponent
    {
        public string Name { get; set; } = "DefaultPlayer";
        public string Description { get; set; } = "This is the default Player object.";
        public string LongName { get; set; } = "Default Player Object";

        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int MaximumHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;

        public string PlayerStorageFileName { get; } = $@"{Constellations.PLAYERSTORAGE}default.json";

        public string PlayerName { get; set; } = "DefaultPlayer";

        public ClientSocket? MyClientSocket { get; private set; }

        public bool CanPickUp { get; set; } = false;

        public bool CanDrop { get; set; } = false;

        public bool CanAdjustSize { get; set; } = true;

        public GameObjectSize Size { get; set; } = GameObjectSize.Medium;

        public float RealWeight { get; set; }

        public override string ToString()
        {
            return PlayerName;
        }

        public void SetSocket(ClientSocket socket)
        {
            MyClientSocket = socket;
        }

        public static async Task SavePlayerAsync(PlayerObject player)
        {
            string filePath = $@"{Constellations.PLAYERSTORAGE}{player.UserName.ToLower()}.json";
            string jsonData = System.Text.Json.JsonSerializer.Serialize(player);
            await File.WriteAllTextAsync(filePath, jsonData);
        }

        public static async Task<PlayerObject?> LoadPlayerAsync(string userName)
        {
            string filePath = $@"{Constellations.PLAYERSTORAGE}{userName.ToLower()}.json";

            if (!File.Exists(filePath))
                return null;

            string jsonData = await File.ReadAllTextAsync(filePath);
            return System.Text.Json.JsonSerializer.Deserialize<PlayerObject>(jsonData);
        }
        public void Initialize()
        {

        }
        public void Update()
        {

        }

        public void AdjustHealth(int amount)
        {
            CurrentHealth -= amount;
            
            if(CurrentHealth < 0)
            {
                //Trigger death
                CurrentHealth = 0;
            }
            
            if(CurrentHealth > MaximumHealth)
            {
                CurrentHealth = MaximumHealth;
            }
        }

        public async Task SendMessageNewLine(string message)
        {
            if (MyClientSocket == null) return;

            try
            {
                await WriteMessage(message + Environment.NewLine);
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task SendMessage(string message)
        {
            try
            {
                await WriteMessage(message);
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task<string> ReceiveMessage()
        {
            if (MyClientSocket == null) return string.Empty;

            try
            {
                string? response = await MyClientSocket.ReceiveANSIMessageAsync();

                if (response == null) return string.Empty;

                return response;
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }

            return string.Empty;
        }

        private async Task WriteMessage(string message)
        {
            if (MyClientSocket == null) return;

            try
            {
                await MyClientSocket.SendMessageAsync(message);
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }
    }
}
