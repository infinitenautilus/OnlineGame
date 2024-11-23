using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using OnlineGame.Core;
using OnlineGame.Network.Client;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Network
{
    public sealed class GateKeeper : ISubsystem
    {
        private static readonly Lazy<GateKeeper> _instance = new(() => new GateKeeper());
        public static GateKeeper Instance => _instance.Value;

        public string Name => "GateKeeper";

        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        public event EventHandler<SystemEventArgs>? StateChanged;

        private GateKeeper() { }

        public void Start()
        {
            CurrentSystemState = SubsystemState.Running;
            RaiseStateChanged("GateKeeper started successfully.");
        }

        public void Stop()
        {
            CurrentSystemState = SubsystemState.Stopped;
            RaiseStateChanged("GateKeeper stopped successfully.");
        }

        public void HandleClient(Socket clientSocket)
        {
            _ = ProcessClientAsync(clientSocket);
        }

        private static async Task ProcessClientAsync(Socket clientSocket)
        {
            var client = new ClientSocket(clientSocket);

            try
            {
                Scribe.Scry($"Handling client: {client.Name}");

                // Add additional client processing logic here
                while (clientSocket.Connected)
                {
                    string message = await client.ReceiveMessageAsync();
                    if (!string.IsNullOrEmpty(message))
                    {
                        Scribe.Scry($"Message from {client.Name}: {message}");
                        await client.SendMessageAsync($"Echo: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error processing client.");
            }
            finally
            {
                client.Disconnect();
                Scribe.Scry($"Client {client.Name} disconnected.");
            }
        }

        private void RaiseStateChanged(string message)
        {
            StateChanged?.Invoke(this, new SystemEventArgs("StateChange", Name, message));
            Scribe.Notification(message);
        }
    }
}
