using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace OnlineGame.Game.Core.Types
{
    public class RoomExit
    {
        // Dictionary<Exit Direction, Exit Room ObjectId Reference>
        [BsonRepresentation(BsonType.ObjectId)]
        public Dictionary<string, string> ExitDirectionDictionary { get; set; } = [];

        /// <summary>
        /// Validates if a direction exists in the exit dictionary.
        /// </summary>
        public bool ValidExit(string direction)
        {
            return ExitDirectionDictionary.ContainsKey(direction);
        }

        /// <summary>
        /// Retrieves the database reference for the room connected to the given direction.
        /// </summary>
        public string ExitFromDirection(string direction)
        {
            if (ExitDirectionDictionary.TryGetValue(direction, out var roomId))
            {
                return roomId;
            }

            return string.Empty;
        }

        /// <summary>
        /// Adds or updates an exit with a direction and corresponding database reference.
        /// </summary>
        public void AddOrUpdateExit(string direction, string roomId)
        {
            ExitDirectionDictionary[direction] = roomId;
        }

        /// <summary>
        /// Removes an exit based on its direction.
        /// </summary>
        public void RemoveExit(string direction)
        {
            ExitDirectionDictionary.Remove(direction);
        }
    }
}
