using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OnlineGame.Game.Interfaces;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace OnlineGame.Game.GameObjects.Things.Rooms
{
    public class RoomObject : IRoomObject
    {
        [BsonId] // Primary key
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = "0";

        public string LongName { get; set; } = "standard room object";
        public string Name { get; set; } = "room";
        public string Description { get; set; } = "Standard room. Nothing fancy.";

        public List<string> Adjectives { get; set; } = [];
        public List<string> Nouns { get; set; } = [];
        public List<string> Aliases { get; set; } = ["Room"];

        public List<string> ExitDirections { get; set; } = [];

        public Vector3 Position { get; set; } = new(0, 0, 0);

        public void Initialize() { }

        public void Update() { }
    }
}
