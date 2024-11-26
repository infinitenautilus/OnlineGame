using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OnlineGame.Core.Types;
using OnlineGame.Game.Core.Processes;
using OnlineGame.Game.Core.Types;
using OnlineGame.Game.Interfaces;
using OnlineGame.Utility.Types;
using System;
using System.Collections.Generic;

namespace OnlineGame.Game.GameObjects.Things.Rooms
{
    public class RoomObject : IRoomObject
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; } = ObjectId.GenerateNewId().ToString();
        public PlayableRegions RoomRegion { get; set; } = PlayableRegions.Arlith;

        public string LongName { get; set; } = "standard room object";
        public string Name { get; set; } = "room";
        public string Description { get; set; } = "Standard room. Nothing fancy.";

        [BsonElement("RoomExits")]
        [BsonIgnoreIfNull]
        public List<RoomExit> RoomExits { get; private set; } = [];
        public ThreadSafeList<GameObject> Contents { get; set; } = [];

        public float MaximumCarryingCapacity { get; set; } = 9999f;
        public bool IsOutdoors { get; set; } = true;

        // Update logic for room, if necessary
        public void Update() { }

        // Methods for manipulating room contents
        public string RoomDescription()
        {
            return Description;
        }

        public void Insert(GameObject item)
        {
            Contents.Add(item);
        }

        public void Withdraw(GameObject item)
        {
            Contents.Remove(item);
        }

        public void EmptyContents()
        {
            Contents.Clear();
        }

        public static async Task<RoomObject> CreateNewRoom(string name, string longName, string description, List<RoomExit> roomExits, bool isOutdoors)
        {
            RoomObject room = new()
            {
                Name = name,
                LongName = longName,
                Description = description,
                RoomExits = roomExits,
                IsOutdoors = isOutdoors
            };

            await RoomRepository.Instance.CreateRoomAsync(room);

            return room;
        }

        public void AddOutdoorExit(string direction, string exitId)
        {
            RoomExit re = new(direction, exitId);

            RoomExits ??= [];

            RoomExits.Add(re);
        }

        public void AddIndoorExit(string direction, string exitId, bool hasDoor, bool isLocked)
        {
            RoomExit re = new(direction, exitId)
            {
                HasDoor = hasDoor,
                LockedExit = isLocked
            };

            RoomExits ??= [];

            RoomExits.Add(re);
        }

        public void RemoveExit(string direction)
        {
            if (RoomExits == null || RoomExits.Count == 0)
                return;

            RoomExits.RemoveAll(re => re.ExitDirection == direction);
        }

        public void RemoveExitById(string roomId)
        {
            if (RoomExits == null || RoomExits.Count == 0)
                return;

            RoomExits.RemoveAll(re => re.ExitId == roomId);
        }
    }
}
