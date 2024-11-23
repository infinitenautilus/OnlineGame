﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OnlineGame.Core;
using OnlineGame.Network.Client;
using OnlineGame.Utility;

namespace OnlineGame.Network
{
    public sealed class Sentinel : ISubsystem
    {
        private static readonly Lazy<Sentinel> _instance = new(() => new Sentinel());
        public static Sentinel Instance => _instance.Value;

        public string Name => "Sentinel";

        private static readonly int _hostPort = Constellations.HOSTPORT;
        private static readonly IPAddress _hostAddress = Constellations.HOSTADDRESS;
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private readonly GateKeeper? _connectionManager;

        private readonly SocketWizard _socketWizard = SocketWizard.Instance;

        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        private CancellationTokenSource _cancellationTokenSource = new();

        public event EventHandler<SystemEventArgs>? StateChanged;

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
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

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

                // Unsubscribe all clients
                _socketWizard.UnsubscribeAll();

                // Safely shutdown and close the socket
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

        private void RaiseStateChanged(string message)
        {
            StateChanged?.Invoke(this, new SystemEventArgs("StateChange", Name, message));
            Scribe.Notification(message);
        }
    }
}