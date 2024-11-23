using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OnlineGame.Core;
using OnlineGame.Network.Client;
using OnlineGame.Utility;

namespace OnlineGame.Network
{
    public sealed class GateKeeper : ISubsystem
    {
        // Singleton instance
        private static readonly Lazy<GateKeeper> _instance = new(() => new GateKeeper());
        public static GateKeeper Instance => _instance.Value;

        private readonly Socket _socket;
        private CancellationTokenSource _cancellationTokenSource = new();
        
        public string Name => "GateKeeper";

        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        public event EventHandler<SystemEventArgs>? StateChanged;

        private GateKeeper()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start()
        {
            try
            {
                if (CurrentSystemState != SubsystemState.Stopped)
                {
                    Scribe.Scry("ConnectionManager is not stopped; ignoring Start().");
                    return;
                }

                CurrentSystemState = SubsystemState.Running;
                _cancellationTokenSource = new CancellationTokenSource();

                // Bind and listen on socket
                _socket.Bind(new System.Net.IPEndPoint(Constellations.HOSTADDRESS, Constellations.HOSTPORT));
                _socket.Listen(10);

                Scribe.Scry($"ConnectionManager is listening on {Constellations.HOSTADDRESS}, port: {Constellations.HOSTPORT}.");
                RaiseStateChanged("ConnectionManager started successfully.");

                _ = HandleConnectionsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error starting ConnectionManager.");
                CurrentSystemState = SubsystemState.Error;
            }
        }

        public void Stop()
        {
            try
            {
                if (CurrentSystemState != SubsystemState.Running)
                {
                    Scribe.Scry("ConnectionManager is not running; ignoring Stop().");
                    return;
                }

                CurrentSystemState = SubsystemState.Stopping;
                _cancellationTokenSource.Cancel();

                // Shutdown the socket safely
                if (_socket.Connected)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (SocketException ex)
                    {
                        Scribe.Error(ex, "Socket shutdown failed in ConnectionManager.");
                    }
                }

                _socket.Close();
                Scribe.Scry("ConnectionManager socket closed.");

                CurrentSystemState = SubsystemState.Stopped;
                RaiseStateChanged("ConnectionManager stopped successfully.");
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error stopping ConnectionManager.");
                CurrentSystemState = SubsystemState.Error;
            }
        }

        private async Task HandleConnectionsAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && CurrentSystemState == SubsystemState.Running)
                {
                    var acceptTask = _socket.AcceptAsync(CancellationToken.None).AsTask();

                    var completedTask = await Task.WhenAny(acceptTask, Task.Delay(-1, token));

                    if (completedTask == acceptTask)
                    {
                        // Handle new connection
                        var newConnection = acceptTask.Result;
                        var clientSocket = new ClientSocket(newConnection);

                        SocketWizard.Instance?.Subscribe(clientSocket);
                        
                        Scribe.Scry($"New client connected: {clientSocket.Name}");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Scribe.Scry("Socket was disposed in ConnectionManager.");
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error in ConnectionManager while handling connections.");
                CurrentSystemState = SubsystemState.Error;
            }
        }

        private void RaiseStateChanged(string message)
        {
            StateChanged?.Invoke(this, new SystemEventArgs("StateChange", Name, message));
            Scribe.Notification(message);
        }
    }
}
