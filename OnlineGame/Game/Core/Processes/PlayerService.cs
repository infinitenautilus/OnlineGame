using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using OnlineGame.Config;
using OnlineGame.Game.GameObjects.Things.Living.Player;
using OnlineGame.Utility;

namespace OnlineGame.Game.Core.Processes 
{ 
    public class PlayerService
    {
        // Singleton instance
        private static readonly Lazy<PlayerService> _instance = new(() => new PlayerService());

        // Private constructor
        private PlayerService() { }

        // Public accessor for the singleton instance
        public static PlayerService Instance => _instance.Value;

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
    }
}
