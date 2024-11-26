
// File: OnlineGame/Game/GameObjects/Player.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OnlineGame.Config;
using OnlineGame.Game.Core.Processes;
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
        public static bool IsGameObject { get; } = true;
        public bool IsLiving { get; set; } = true;
        public string Name { get; set; } = "player_object";
        public string Description { get; set; } = "This is the default Player object.";
        public string LongName { get; set; } = "Default Player Object";

        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public int MaximumHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;

        public string PlayerName { get; set; } = "Bob";

        public bool CanPickUp { get; set; } = false;

        public bool CanDrop { get; set; } = false;

        public bool CanAdjustSize { get; set; } = true;

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

        public async Task SendMessageAsync(string message)
        {
            try
            {
                if (MyClientSocket == null)
                    return;

                await MyClientSocket.WriteLineAsync(message);
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task<string> ReadMessageAsync()
        {
            if(MyClientSocket == null)
            {
                return string.Empty;
            }

            try
            {
                string message = await MyClientSocket.ReceiveAsync();

                return message;
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
                return string.Empty;
            }
        }
    }
}
