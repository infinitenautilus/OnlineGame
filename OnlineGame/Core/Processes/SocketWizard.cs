using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Types;
using OnlineGame.Network;
using OnlineGame.Network.Client;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Core.Processes
{
    public sealed class SocketWizard : ISubsystem, IUpdateable
    {
        // Singleton instance
        private static readonly Lazy<SocketWizard> _instance = new(() => new SocketWizard());
        public static SocketWizard Instance => _instance.Value;

        // Manage the Socket pulse to ping and check for disconnected users.
        // This uses an independent heartbeat.

        public Heartbeat KeepAliveTimer { get; private set; } = new(500);


        // Current connected clients
        private readonly ThreadSafeList<ClientSocket> _currentClients = [];
        public IReadOnlyCollection<ClientSocket> CurrentClients => [.. _currentClients];

        public string Name => "SocketWizard";

        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        public event EventHandler<SystemEventArgs>? StateChanged;

        // Private constructor to enforce singleton
        private SocketWizard()
        {
        }

        public void Start()
        {
            KeepAliveTimer.Register(this);
            KeepAliveTimer.Start();

            CurrentSystemState = SubsystemState.Running;
            NotifyStateChanged("Socket Wizard started successfully.");
        }

        public void Stop()
        {
            KeepAliveTimer?.Stop();
            CurrentSystemState = SubsystemState.Stopped;
            NotifyStateChanged("Socket Wizard stopped successfully.");
        }

        public void Update()
        {
            List<ClientSocket> temp = new([.. CurrentClients]);
            
            foreach (ClientSocket cs in temp)
            {
                cs.Update();
            }
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
                    KeepAliveTimer.Register(client);
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

        public async void Unsubscribe(ClientSocket client)
        {
            try
            {
                if (_currentClients.Remove(client))
                {
                    Scribe.Notification($"Client {client.Name} unsubscribed.");
                    await BroadcastMessage($"Client {client.Name} unsubscribed.");

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
                foreach (ClientSocket client in _currentClients.ToList()) // Safe enumeration
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
                foreach (ClientSocket client in _currentClients.ToList())
                {
                    await client.WriteLineAsync(message);
                }

            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        private void NotifyStateChanged(string message)
        {
            StateChanged?.Invoke(this, new SystemEventArgs("StateChange", Name, message));
            Scribe.Notification(message);
        }
    }
}
