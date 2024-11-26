using System;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OnlineGame.Core;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Processes;
using OnlineGame.Utility;

namespace OnlineGame.Network.Client
{
    public class ClientSocket(Socket newSocket) : IDisposable, IUpdateable
    {
        private readonly Socket _socket = newSocket ?? throw new ArgumentNullException(nameof(newSocket));
        private bool _disposed;

        public int TerminalColumns { get; set; } = 80;

        public static Guid Id { get; } = Guid.NewGuid();
        public string Name { get; private set; } = $"Client_{Id}";

        public string GetSocketAddressDNS()
        {
            try
            {
                // Ensure the socket is connected
                if (_socket?.Connected != true)
                    return "Unknown";

                // Get the remote endpoint associated with the socket
                EndPoint? remoteEndPoint = _socket.RemoteEndPoint;

                if (remoteEndPoint is IPEndPoint ipEndPoint)
                {
                    // Resolve DNS for the IP address
                    IPHostEntry entry = Dns.GetHostEntry(ipEndPoint.Address);
                    return entry.HostName; // Return the resolved hostname
                }

                return "Unknown";
            }
            catch (SocketException ex)
            {
                HandleDisconnection();
                return $"Socket error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public bool Connected
        {
            get
            {
                if (_disposed) return false;
                return IsSocketConnected();
            }
        }

        public async Task<IPAddress> GetIPAddress()
        {
            try
            {
                // Ensure the RemoteEndPoint is not null and is of type IPEndPoint.
                if (newSocket?.RemoteEndPoint is IPEndPoint remoteEndPoint)
                {
                    return remoteEndPoint.Address;
                }

                // If RemoteEndPoint is null or not an IPEndPoint, notify and handle gracefully.
                Scribe.Notification($"User {Name} had no valid IP.");
                await WriteLineAsync("Sorry you don't have a valid network address.");

                return IPAddress.Parse("0.0.0.0");
            }
            catch (Exception ex)
            {
                // Log any unexpected errors for debugging.
                Scribe.Error(ex);
                Disconnect();
                return IPAddress.Parse("0.0.0.0");
            }


        }


        public async Task<string> GetSocketAddressStringAsync()
        {
            IPAddress ipAddress = await GetIPAddress();
            return ipAddress.ToString();
        }


        public event EventHandler? Disconnected;
        public event EventHandler<SocketException>? SocketError;

        // Lazy function to write out
        public async Task WriteAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                    throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

                string filtered = UniversalTranslator.Instance.TranslateMessageToANSI(message);

                byte[] buffer = Encoding.UTF8.GetBytes(filtered);

                if (!IsSocketConnected())
                {
                    Disconnect();
                    return;
                }

                await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (SocketException ex)
            {
                Scribe.Error(ex);
            }
            catch (OperationCanceledException)
            {
                Scribe.Scry("SendMessageAsync operation was canceled.");
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        // Lazy function to write with new line to client
        public async Task WriteLineAsync(string message)
        {
            try
            {
                await WriteAsync(message + Environment.NewLine);
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        // Lazy function receive a string
        public async Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[1024];

            try
            {
                // Check if the socket is connected
                if (!IsSocketConnected())
                {
                    HandleDisconnection();
                    return string.Empty;
                }

                // Receive data from the socket
                int bytesReceived = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);

                // Handle disconnection if no bytes were received
                if (bytesReceived == 0)
                {
                    HandleDisconnection();
                    return string.Empty;
                }

                // Decode the message
                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesReceived);

                // Clean up Telnet-specific artifacts and trim the message
                string telnetCleanedMessage = UniversalTranslator.CleanTelnetInput(receivedMessage, TerminalColumns);

                // Ignore empty or whitespace-only messages
                if (string.IsNullOrWhiteSpace(telnetCleanedMessage))
                {
                    return string.Empty;
                }

                return telnetCleanedMessage;
            }
            catch (SocketException)
            {
                // Handle socket exceptions (e.g., reconnect or cleanup)
                await MonitorSocketAsync();
                return string.Empty;
            }
            catch (OperationCanceledException)
            {
                // Log cancellation of the receive operation
                Scribe.Scry("ReceiveMessageAsync operation was canceled.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                // Log any other exceptions
                Scribe.Error(ex);
                return string.Empty;
            }
        }

        public void Disconnect()
        {
            if (_disposed) return;

            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException ex)
            {
                HandleSocketException(ex);
            }
            catch (ObjectDisposedException)
            {
                // Socket is already disposed
            }
            finally
            {
                if (SocketWizard.Instance.CurrentClients.ToList().Contains(this))
                    SocketWizard.Instance.Unsubscribe(this);

                Scribe.Notification($"Client Disconnecting: {Name} from {GetSocketAddressDNS()}");
                HandleDisconnection();
                Dispose();
            }
        }

        public void Update()
        {
            if (_disposed) return;

            if(!IsSocketConnected())
            {
                HandleDisconnection();
            }
        }

        private async Task MonitorSocketAsync()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytesRead = await _socket.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Socket disconnected.");
                        break;
                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Socket disconnected due to an error.");
                Disconnect();
            }
        }


        private void HandleDisconnection()
        {
            if (_disposed) return;

            // Notify external handlers about the disconnection
            OnDisconnected();

            // Notify the SocketWizard singleton
            SocketWizard.Instance.Unsubscribe(this);

            Dispose();
        }

        private void HandleSocketException(SocketException ex)
        {
            Scribe.Error(ex, "SocketException occurred.");
            SocketError?.Invoke(this, ex);

            HandleDisconnection();
        }

        private void OnDisconnected()
        {
            // Trigger the Disconnected event
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private bool IsSocketConnected()
        {
            try
            {
                return !_socket.Poll(1000, SelectMode.SelectRead) || _socket.Available != 0;
            }
            catch
            {
                HandleDisconnection();
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch
            {
                // Ignored
            }
            finally
            {
                _socket.Close();
                _socket.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~ClientSocket()
        {
            Dispose();
        }
    }

}
