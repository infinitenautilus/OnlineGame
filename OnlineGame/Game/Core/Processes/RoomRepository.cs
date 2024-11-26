using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using OnlineGame.Config;
using OnlineGame.Game.Core.Types;
using OnlineGame.Game.GameObjects.Things.Rooms;
using OnlineGame.Utility;

namespace OnlineGame.Game.Core.Processes
{
    public sealed class RoomRepository
    {
        private static readonly Lazy<RoomRepository> _instance = new(() => new RoomRepository());
        private readonly IMongoCollection<RoomObject> _rooms;

        // Private constructor to enforce singleton pattern
        private RoomRepository()
        {
            MongoClient client = new(Constellations.DATABASECONNECTIONSTRING);
            IMongoDatabase database = client.GetDatabase("OnlineGame");
            _rooms = database.GetCollection<RoomObject>("Rooms");
        }

        // Public property to access the singleton instance
        public static RoomRepository Instance => _instance.Value;

        public async Task CreateRoomAsync(RoomObject room)
        {
            await _rooms.InsertOneAsync(room);
        }

        public async Task<List<RoomObject>> GetAllRoomsAsync()
        {
            return await _rooms.Find(FilterDefinition<RoomObject>.Empty).ToListAsync();
        }

        /// <summary>
        /// Reserved to be called by SystemWizard when starting all the processes
        /// </summary>
        public async Task Start()
        {
            RoomObject? room = await GetRoomByIdAsync("674631058f71c2e83f0202f3");

            if (room == null) return;

            room.AddIndoorExit("north", "674631058f71c2e83f0202f4", false, false);

            await UpdateRoomAsync(room);

            Console.WriteLine("Exit should be added.");
        }
        public async Task<RoomObject?> GetRoomByIdAsync(string id)
        {
            return await _rooms.Find(r => r.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateRoomAsync(RoomObject room)
        {
            Console.WriteLine("UpdateRoomAsync Called");

            Console.WriteLine($"Room ID: {room.Id}");
            Console.WriteLine($"Room Exits Count: {room.RoomExits?.Count ?? 0}");

            foreach (var exit in room.RoomExits ?? [])
            {
                Console.WriteLine($"Exit Direction: {exit.ExitDirection}, Exit ID: {exit.ExitId}");
            }

            await _rooms.ReplaceOneAsync(r => r.Id == room.Id, room);
        }

        public async Task DeleteRoomAsync(string id)
        {
            await _rooms.DeleteOneAsync(r => r.Id == id);
        }
    }
}
