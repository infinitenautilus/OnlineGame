using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Types;
using OnlineGame.Network.Client;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Network
{
    public sealed class SocketWizard : ISubsystem
    {
        // Singleton instance
        private static readonly Lazy<SocketWizard> _instance = new(() => new SocketWizard());
        public static SocketWizard Instance => _instance.Value;

        // Current connected clients
        private readonly ThreadSafeList<ClientSocket> _currentClients = new();
        public IReadOnlyCollection<ClientSocket> CurrentClients => _currentClients.ToList();

        public string Name => "SocketWizard";

        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        public event EventHandler<SystemEventArgs>? StateChanged;

        // Private constructor to enforce singleton
        private SocketWizard()
        {
        }

        public void Start()
        {
            CurrentSystemState = SubsystemState.Running;
            NotifyStateChanged("Socket Wizard started successfully.");
        }

        public void Stop()
        {
            CurrentSystemState = SubsystemState.Stopped;
            NotifyStateChanged("Socket Wizard stopped successfully.");
        }

        public async Task Subscribe(ClientSocket client)
        {
            try
            {

                if (!_currentClients.Contains(client))
                {
                    _currentClients.Add(client);

                    await BroadcastMessage($"New Client Subscribed: {client.Name}");
                    NotifyStateChanged($"Added new Client: {client.Name}");
                }
                else
                {
                    Scribe.Scry($"Tried to subscribe an existing connection {Name}");
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error in Subscribe.");
            }
        }

        public void Unsubscribe(ClientSocket client)
        {
            try
            {
                if (_currentClients.Remove(client))
                {
                    Scribe.Notification($"Client {client.Name} unsubscribed.");
                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error in Unsubscribe.");
            }
        }

        public void UnsubscribeAll()
        {
            try
            {
                foreach (var client in _currentClients.ToList()) // Safe enumeration
                {
                    Unsubscribe(client);
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error in UnsubscribeAll.");
            }
        }

        public async Task BroadcastMessage(string message)
        {
            try
            {
                foreach (var client in _currentClients.ToList())
                {
                    await client.SendMessageAsync(message);
                }
             
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error in BroadcastMessage.");
            }
        }

        private void NotifyStateChanged(string message)
        {
            StateChanged?.Invoke(this, new SystemEventArgs("StateChange", Name, message));
            Scribe.Notification(message);
        }
    }
}
