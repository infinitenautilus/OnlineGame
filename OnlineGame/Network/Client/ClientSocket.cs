using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OnlineGame.Core;
using OnlineGame.Core.Processes;
using OnlineGame.Utility;

namespace OnlineGame.Network.Client
{
    public class ClientSocket(Socket newSocket) : IDisposable
    {
        private readonly Socket _socket = newSocket ?? throw new ArgumentNullException(nameof(newSocket));
        private bool _disposed;

        public static Guid Id { get; } = Guid.NewGuid();
        public string Name { get; private set; } = $"Client_{Id}";
        public string SocketAddressDns 
        {
            get
            {
                if (newSocket.RemoteEndPoint is IPEndPoint remoteEndPoint)
                {
                    try
                    {
                        IPHostEntry hostEntry = Dns.GetHostEntry(remoteEndPoint.Address);

                        return hostEntry.HostName; // Returns the DNS host name
                    }
                    catch(Exception)
                    {
                        return "Unknown";
                    }
                }

                return "Unknown";
            }

        }

        public async Task<IPAddress?> SocketIPAddress()
        {
            if (newSocket.RemoteEndPoint is IPEndPoint remoteEndPoint)
            {
                return remoteEndPoint.Address; // Extract the IPAddress from the remote endpoint
            }
            else
            {
                Scribe.Notification($"User {Name} had no valid IP.");
                await SendMessageAsync("Sorry you have no valid IP.");

                Disconnect();
                return null; // Return null if the RemoteEndPoint is not set
            }
        }


        public string? SocketAddressString 
        {
            get
            {
                return SocketIPAddress()?.ToString();
            }
        }
        


        public event EventHandler? Disconnected;
        public event EventHandler<SocketException>? SocketError;

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

            byte[] buffer = Encoding.UTF8.GetBytes(message + Environment.NewLine);

            try
            {
                if (!IsSocketConnected())
                {
                    HandleDisconnection();
                    return;
                }

                await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (SocketException ex)
            {
                HandleSocketException(ex);
            }
            catch (OperationCanceledException)
            {
                Scribe.Scry("SendMessageAsync operation was canceled.");
            }
            catch (Exception ex)
            {
                HandleGeneralException(ex, nameof(SendMessageAsync));
            }
        }

        public async Task<string> ReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[1024];

            try
            {
                if (!IsSocketConnected())
                {
                    HandleDisconnection();
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
            catch (SocketException ex)
            {
                HandleSocketException(ex);
                return string.Empty;
            }
            catch (OperationCanceledException)
            {
                Scribe.Scry("ReceiveMessageAsync operation was canceled.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                HandleGeneralException(ex, nameof(ReceiveMessageAsync));
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
                HandleDisconnection();
                Dispose();
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

        private static void HandleGeneralException(Exception ex, string context)
        {
            Scribe.Error(ex, $"Unexpected error in {context}.");
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
