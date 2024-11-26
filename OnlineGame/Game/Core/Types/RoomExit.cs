using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace OnlineGame.Game.Core.Types
{
    public class RoomExit
    {
        public RoomExit(string exitDirectionName, string exitId)
        {
            ExitDirection = exitDirectionName;
            ExitId = exitId;
            HasDoor = false;
            LockedExit = false;
        }

        public RoomExit(string exitDirectionName, string exitId, bool isDoor, bool isLocked)
        {
            ExitDirection = exitDirectionName;
            ExitId = exitId;
            HasDoor = isDoor;
            LockedExit = isLocked;
        }

        public string ExitDirection { get; set; } = string.Empty;
        public string ExitId { get; set; } = string.Empty;

        public bool HasDoor { get; set; } = false;
        public bool LockedExit { get; set; } = false;

    }
}
