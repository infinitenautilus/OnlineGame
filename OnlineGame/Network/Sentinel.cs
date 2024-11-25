using OnlineGame.Network;
using OnlineGame.Utility.Types;
using OnlineGame.Utility;
using System.Net.Sockets;
using System.Net;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Types;
using OnlineGame.Network.Client;
using OnlineGame.Config;

namespace OnlineGame.Network
{
    /// <summary>
    /// The <c>Sentinel</c> class is responsible for managing the network listening subsystem, 
    /// handling client connections, and maintaining subsystem states.
    /// </summary>
    public sealed class Sentinel : ISubsystem
    {
        private static readonly Lazy<Sentinel> _instance = new(() => new Sentinel());

        /// <summary>
        /// Gets the singleton instance of the <c>Sentinel</c> class.
        /// </summary>
        public static Sentinel Instance => _instance.Value;

        /// <inheritdoc/>
        public string Name => "Sentinel";

        private static readonly int _hostPort = Constellations.HOSTPORT;
        private static readonly IPAddress _hostAddress = Constellations.HOSTADDRESS;

        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        /// <summary>
        /// Gets the current state of the subsystem.
        /// </summary>
        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        private CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Occurs when the state of the subsystem changes.
        /// </summary>
        public event EventHandler<SystemEventArgs>? StateChanged;

        /// <summary>
        /// Initializes a new instance of the <c>Sentinel</c> class.
        /// </summary>
        private Sentinel() { }

        /// <summary>
        /// Starts the <c>Sentinel</c>, initializing the socket listener and accepting client connections asynchronously.
        /// </summary>
        public void Start()
        {
            try
            {
                if (CurrentSystemState != SubsystemState.Stopped)
                {
                    Scribe.Scry("The CurrentSystemState was not stopped, but a Start() was called. Ignoring.");
                    return;
                }

                CurrentSystemState = SubsystemState.Running;
                _cancellationTokenSource = new CancellationTokenSource();

                IPEndPoint ep = new(_hostAddress, _hostPort);

                _socket.Bind(ep);
                _socket.Listen(10);

                Scribe.Scry($"Listening for connections on {_hostAddress}, port: {_hostPort}");
                RaiseStateChanged("Sentinel started successfully.");

                _ = AcceptConnectionsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
                CurrentSystemState = SubsystemState.Error;
            }
        }

        /// <summary>
        /// Stops the <c>Sentinel</c>, shutting down the socket and canceling connection acceptance tasks.
        /// </summary>
        public void Stop()
        {
            try
            {
                if (CurrentSystemState != SubsystemState.Running)
                {
                    Scribe.Scry("The CurrentSystemState was not running, but a Stop() was called. Ignoring.");
                    return;
                }

                CurrentSystemState = SubsystemState.Stopping;

                // Signal cancellation to all tasks
                _cancellationTokenSource.Cancel();

                // Close and shutdown the socket
                if (_socket.Connected)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (SocketException ex)
                    {
                        Scribe.Error(ex, "Socket shutdown failed. Socket may already be disconnected.");
                    }
                }
                else
                {
                    Scribe.Scry("Socket is already disconnected.");
                }

                _socket.Close();
                Scribe.Scry("Sentinel socket has been closed.");

                CurrentSystemState = SubsystemState.Stopped;
                RaiseStateChanged("Sentinel ended successfully.");
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error stopping Sentinel.");
                CurrentSystemState = SubsystemState.Error;
            }
        }

        /// <summary>
        /// Asynchronously accepts incoming client connections and delegates handling to the <c>GateKeeper</c>.
        /// </summary>
        /// <param name="token">A cancellation token to manage task cancellation.</param>
        private async Task AcceptConnectionsAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var acceptTask = _socket.AcceptAsync(CancellationToken.None).AsTask();
                    var completedTask = await Task.WhenAny(acceptTask, Task.Delay(-1, token));

                    if (completedTask == acceptTask)
                    {
                        Socket clientSocket = acceptTask.Result;

                        Scribe.Scry($"New client accepted.");

                        // Pass client to GateKeeper for further handling
                        GateKeeper.Instance.HandleClient(clientSocket);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Expected during shutdown
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Error accepting connections in Sentinel.");
            }
        }

        /// <summary>
        /// Raises the <see cref="StateChanged"/> event and logs the state change notification.
        /// </summary>
        /// <param name="message">The message describing the state change.</param>
        private void RaiseStateChanged(string message)
        {
            StateChanged?.Invoke(this, new SystemEventArgs("StateChange", Name, message));
            Scribe.Notification(message);
        }
    }
}
