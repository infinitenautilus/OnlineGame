using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using OnlineGame.Config;
using OnlineGame.Game.GameObjects.Things.Living.Player;
using OnlineGame.Utility;
using OnlineGame.Core.Processes;
using OnlineGame.Utility.Types;

namespace OnlineGame.Game.Core.Processes 
{ 
    public static class PlayerService
    {
        public static ThreadSafeList<PlayerObject> ActivePlayers { get; private set; } = [];
        
        // MongoDB collection retrieval
        private static IMongoCollection<PlayerObject> GetPlayerCollection()
        {
            MongoClient client = new(Constellations.DATABASECONNECTIONSTRING);
            IMongoDatabase database = client.GetDatabase(Constellations.DATABASENAMESTRING);

            return database.GetCollection<PlayerObject>("Players");
        }

        // Save or update a player in the database
        public static async Task SavePlayerAsync(PlayerObject player)
        {
            try
            {
                IMongoCollection<PlayerObject> collection = GetPlayerCollection();
                FilterDefinition<PlayerObject> filter = Builders<PlayerObject>.Filter.Eq(p => p.UserName, player.UserName);

                PlayerObject existingPlayer = await collection.Find(filter).FirstOrDefaultAsync();

                if (existingPlayer == null)
                {
                    Console.WriteLine("Existing player not found. Inserting new player.");
                    await collection.InsertOneAsync(player);
                }
                else
                {
                    player.Id = existingPlayer.Id; // Preserve existing ID
                    Console.WriteLine("Existing player found. Replacing with updated data.");
                    await collection.ReplaceOneAsync(filter, player);
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error saving player.");
            }
        }

        // Load a player from the database by UserName
        public static async Task<PlayerObject> LoadPlayerAsync(string userName)
        {
            try
            {
                IMongoCollection<PlayerObject> collection = GetPlayerCollection();
                FilterDefinition<PlayerObject> filter = Builders<PlayerObject>.Filter.Eq(p => p.UserName, userName);

                var player = await collection.Find(filter).FirstOrDefaultAsync();
                return player;
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, $"Error loading player with UserName: {userName}");
                return new PlayerObject() { Name = "failed" };
            }

        
        }

        // Check if a UserName exists in the database
        public static async Task<bool> UserNameExistsAsync(string userName)
        {
            try
            {
                IMongoCollection<PlayerObject> collection = GetPlayerCollection();
                FilterDefinition<PlayerObject> filter = Builders<PlayerObject>.Filter.Eq(p => p.UserName, userName);

                return await collection.Find(filter).AnyAsync();
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, $"Error checking if UserName exists: {userName}");
                return false;
            }
        }

        public static async Task<PlayerObject> InitializeNewPlayer(string userName, string password)
        {
            if(await UserNameExistsAsync(userName))
            {
                return await LoadPlayerAsync(userName);
            }

            PlayerObject temporaryPlayerObject = new();
            userName = userName.ToLower();
            string properName = $"{char.ToUpper(userName[0])}{userName[1..]}";

            temporaryPlayerObject.PasswordHash = CommunicationsOperator.HashPassword(password);
            temporaryPlayerObject.UserName = userName;
            temporaryPlayerObject.Description = "A handsome lad or lass or non-binary pal.";
            temporaryPlayerObject.Name = properName;
            temporaryPlayerObject.LongName = properName + " the Adventurer";

            return temporaryPlayerObject;
        }

        public static void Subscribe(PlayerObject player)
        {
            ActivePlayers.Add(player);
        }

        public static void Unsubscribe(PlayerObject player)
        {
            ActivePlayers.Remove(player);
        }

        public static void UnsubscribeAll()
        {
            var tempList = ActivePlayers;

            foreach(PlayerObject player in tempList)
            {
                Unsubscribe(player);
            }
        }
    }
}
