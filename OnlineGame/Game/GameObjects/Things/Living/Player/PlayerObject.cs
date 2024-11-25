
// File: OnlineGame/Game/GameObjects/Player.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OnlineGame.Config;
using OnlineGame.Game.Core.Types;
using OnlineGame.Game.GameObjects.ComponentInterfaces;
using OnlineGame.Game.Interfaces;
using OnlineGame.Network.Client;
using OnlineGame.Utility;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Numerics;

namespace OnlineGame.Game.GameObjects.Things.Living.Player
{
    public class PlayerObject : IPlayerObject
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public ClientSocket? MyClientSocket { get; private set; }
        public bool IsGameObject { get; } = true;
        public string Name { get; set; } = "player_object";
        public string Description { get; set; } = "This is the default Player object.";
        public string LongName { get; set; } = "Default Player Object";

        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int MaximumHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;

        public string PlayerStorageFileName { get; } = $@"{Constellations.PLAYERSTORAGE}default.json";

        public string PlayerName { get; set; } = "Bob";

        public bool CanPickUp { get; set; } = false;

        public bool CanDrop { get; set; } = false;

        public bool CanAdjustSize { get; set; } = true;

        public List<string> Adjectives { get; set; } = [];
        public List<string> Nouns { get; set; } = [];
        public List<string> Aliases { get; set; } = ["Player"];

        public GameObjectSize Size { get; set; } = GameObjectSize.Medium;
        public float RealWeight { get; set; } = 1000f;

        public override string ToString()
        {
            return PlayerName;
        }

        public void SetSocket(ClientSocket socket)
        {
            MyClientSocket = socket;
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

            if (CurrentHealth < 0)
            {
                //Trigger death
                CurrentHealth = 0;
            }

            if (CurrentHealth > MaximumHealth)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
                await MyClientSocket.SendANSIMessageAsync(message);
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        private static IMongoCollection<PlayerObject> GetPlayerCollection()
        {
            MongoClient client = new(Constellations.DATABASECONNECTIONSTRING);
            IMongoDatabase database = client.GetDatabase(Constellations.DATABASENAMESTRING);

            return database.GetCollection<PlayerObject>("Players");
        }

        public static async Task SavePlayerAsync(PlayerObject player)
        {
            try
            {
                IMongoCollection<PlayerObject> collection = GetPlayerCollection();
                FilterDefinition<PlayerObject> filter = Builders<PlayerObject>.Filter.Eq(p => p.UserName, player.UserName);

                PlayerObject existingPlayer = await collection.Find(filter).FirstOrDefaultAsync();

                if (existingPlayer == null)
                {
                    Console.WriteLine("Existing player not found.");
                    await collection.InsertOneAsync(player);
                }
                else
                {
                    player.Id = existingPlayer.Id;
                    Console.WriteLine("Existing player found.");
                    await collection.ReplaceOneAsync(filter, player);
                }
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public static async Task<PlayerObject?> LoadPlayerAsync(string userName)
        {
            try
            {
                IMongoCollection<PlayerObject> collection = GetPlayerCollection();
                FilterDefinition<PlayerObject> filter = Builders<PlayerObject>.Filter.Eq(p => p.UserName, userName);

                return await collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);

                return null;
            }
        }

        public static async Task<bool> UserNameExistsAsync(string userName)
        {
            try
            {
                IMongoCollection<PlayerObject> collection = GetPlayerCollection();
                FilterDefinition<PlayerObject> filter = Builders<PlayerObject>.Filter.Eq(p => p.UserName, userName);

                var userExists = await collection.Find(filter).AnyAsync();

                if(userExists)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, $"Error checking if userName exists: {userName}");
                return false;
            }
        }

    }
}
