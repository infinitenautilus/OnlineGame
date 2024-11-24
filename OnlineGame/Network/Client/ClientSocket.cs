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
                await WriteMessageAsync("Sorry, you have no valid IP.");

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

        public async Task SendClearScreenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                const string clearScreenCommand = "\x1b[2J"; // ANSI escape code to clear screen
                await WriteMessageAsync(clearScreenCommand, cancellationToken);
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            await WriteMessageAsync(message, cancellationToken);
        }

        public async Task SendMessageLineAsync(string message, CancellationToken cancellationToken = default)
        {
            await WriteMessageAsync(message + Environment.NewLine, cancellationToken);
        }

        public async Task SendANSIMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            string filtered;
            
            filtered = UniversalTranslator.Instance.TranslateMessage(message);

            await WriteMessageAsync(filtered, cancellationToken);
        }

        private async Task WriteMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

            byte[] buffer = Encoding.UTF8.GetBytes(message);


            try
            {
                if(!IsSocketConnected())
                {
                    Disconnect();
                    return;
                }

                await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch(SocketException ex)
            {
                Scribe.Error(ex);
            }
            catch(OperationCanceledException)
            {
                Scribe.Scry("SendMessageAsync operation was canceled.");
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task<string> ReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            return await ReceiveRawMessageAsync(cancellationToken);
        }

        private async Task<string> ReceiveRawMessageAsync(CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[1024];

            try
            {
                if (!IsSocketConnected())
                {
                    Disconnect();
                    return string.Empty;
                }

                int bytesReceived = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);

                if (bytesReceived == 0)
                {
                    HandleDisconnection();
                    return string.Empty;
                }

                return Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            }
            catch (SocketException)
            {
                await MonitorSocketAsync();
                return string.Empty;
            }
            catch (OperationCanceledException)
            {
                Scribe.Scry("ReceiveMessageAsync operation was canceled.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
                return string.Empty;
            }
        }

        public async Task<string> ReceiveANSIMessageAsync(CancellationToken cancellationToken= default)
        {
            string unfiltered = await ReceiveRawMessageAsync(cancellationToken);

            string filtered = UniversalTranslator.Instance.TranslateMessage(unfiltered);

            return filtered;
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
                Console.WriteLine("Client disconnected.");
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
