using System;
using System.Linq;
using System.Threading.Tasks;
using OnlineGame.Game.GameObjects.Things.Living.Player;
using OnlineGame.Utility.Types;
using OnlineGame.Utility;

namespace OnlineGame.Game.Core.Processes
{
    /// <summary>
    /// SpiritKeeper acts as the intermediary layer between player client information 
    /// and their in-game character representation. It manages a threadsafe list of PlayerObjects.
    /// </summary>
    public sealed class SpiritKeeper
    {
        // Singleton instance
        private static readonly Lazy<SpiritKeeper> _instance = new(() => new SpiritKeeper());
        public static SpiritKeeper Instance => _instance.Value;

        // Thread-safe list to manage subscribed PlayerObjects
        private readonly ThreadSafeList<PlayerObject> _playerObjects = [];

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private SpiritKeeper() { }

        /// <summary>
        /// Adds a PlayerObject to the subscribed list.
        /// </summary>
        /// <param name="player">The PlayerObject to subscribe.</param>
        public void Subscribe(PlayerObject player)
        {
            if (!_playerObjects.Contains(player))
            {
                _playerObjects.Add(player);
                Scribe.Notification($"Player {player.UserName} subscribed to SpiritKeeper.");
            }
            else
            {
                Scribe.Scry($"Player {player.UserName} is already subscribed to SpiritKeeper.");
            }
        }

        /// <summary>
        /// Removes a PlayerObject from the subscribed list.
        /// </summary>
        /// <param name="player">The PlayerObject to unsubscribe.</param>
        public void Unsubscribe(PlayerObject player)
        {
            if (_playerObjects.Remove(player))
            {
                Scribe.Notification($"Player {player.UserName} unsubscribed from SpiritKeeper.");
            }
            else
            {
                Scribe.Scry($"Player {player.UserName} was not subscribed to SpiritKeeper.");
            }
        }

        /// <summary>
        /// Retrieves the PlayerObject for a given username.
        /// </summary>
        /// <param name="userName">The username of the player to retrieve.</param>
        /// <returns>The PlayerObject if found; otherwise, null.</returns>
        public PlayerObject? GetPlayerByUserName(string userName)
        {
            return _playerObjects.FirstOrDefault(p => p.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Asynchronously notifies all subscribed players with a message.
        /// </summary>
        /// <param name="message">The message to broadcast.</param>
        public async Task NotifyAllPlayersAsync(string message)
        {
            foreach (PlayerObject? player in _playerObjects)
            {
                try
                {
                    if (player.MyClientSocket == null)
                        throw new NullReferenceException(nameof(player.MyClientSocket));

                    // Assuming PlayerObject has a reference to its ClientSocket for sending messages
                    await player.MyClientSocket.WriteLineAsync(message);
                }
                catch (Exception ex)
                {
                    Scribe.Error(ex, $"Error notifying player {player.UserName}.");
                }
            }
        }

        /// <summary>
        /// Gets the current count of subscribed players.
        /// </summary>
        /// <returns>The number of subscribed PlayerObjects.</returns>
        public int GetSubscribedPlayerCount()
        {
            return _playerObjects.Count;
        }
    }
}
