using System;
using System.Threading.Tasks;
using OnlineGame.Core;
using OnlineGame.Core.Interfaces;
using OnlineGame.Game.GameObjects.Things.Living.Player;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Game.Core.Processes
{
    public class GameDirector : IUpdateable
    {
        private static readonly Lazy<GameDirector> _instance = new(() => new GameDirector());
        public static GameDirector Instance => _instance.Value;

        public ThreadSafeList<PlayerObject> ActivePlayers { get; private set; } = [];
        public Heartbeat Pulse { get; private set; }
        private bool isRunning = false;

        private GameDirector()
        {
            Pulse = new Heartbeat(500);
        }

        public void Subscribe(PlayerObject player)
        {
            ActivePlayers.Add(player);
            Pulse.Register(player);
        }

        public void Unsubscribe(PlayerObject player)
        {
            ActivePlayers.Remove(player);
            Pulse.Unregister(player);
        }

        public void UnsubscribeAll()
        {
            foreach (PlayerObject p in ActivePlayers)
            {
                Pulse.Unregister(p);
            }
            ActivePlayers.Clear();
        }

        public async Task EnterGameLoop(PlayerObject player)
        {
            Subscribe(player);
            await MainGameLoop(player);
        }

        public void Start()
        {
            Scribe.Notification("Game Director is awake.");
            Pulse.Register(this, UpdateGameLoop); // Register a custom action
            Pulse.Start();
            isRunning = true;
        }

        public void Stop()
        {
            Pulse.Stop();
            isRunning = false;
            UnsubscribeAll();
            Scribe.Notification("Game Director is offline.");
        }

        private async Task MainGameLoop(PlayerObject player)
        {
            while (isRunning)
            {
                await Task.Delay(500); // Simulated game logic
                await player.SendMessageAsync("Update!");
            }
        }

        // Custom method to be executed by the Heartbeat
        private void UpdateGameLoop()
        {
            foreach (PlayerObject player in ActivePlayers)
            {
                // Perform game logic for each active player
                Scribe.Notification($"Updating player {player.UserName}");
            }
        }

        public void Update()
        {
            // This method is not used in this implementation
        }
    }
}
