using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OnlineGame.Core.Processes;
using OnlineGame.Utility;

namespace OnlineGame.Network.Client
{
    public class ClientSocket(Socket newSocket) : IDisposable
    {
        private readonly Socket _socket = newSocket;
        private bool _disposed;

        public static Guid Id { get; } = Guid.NewGuid();
        public string Name { get; private set; } = $"Client_{Id}";


        public event EventHandler? Disconnected = (s, e) => { };
        public event EventHandler<SocketException>? SocketError = (s, e) => { };

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

            byte[] buffer = Encoding.UTF8.GetBytes(message + Environment.NewLine);

            try
            {
                await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Scribe.Scry("SendMessageAsync operation was canceled.");
            }
            catch (SocketException ex)
            {
                HandleSocketException(ex);
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
                int bytesReceived = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);

                if (bytesReceived == 0)
                {
                    OnDisconnected();
                    return string.Empty;
                }

                return Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            }
            catch (OperationCanceledException)
            {
                Scribe.Scry("ReceiveMessageAsync operation was canceled.");
                return string.Empty;
            }
            catch (SocketException ex)
            {
                HandleSocketException(ex);
                return string.Empty;
            }
            catch (Exception ex)
            {
                HandleGeneralException(ex, nameof(ReceiveMessageAsync));
                return string.Empty;
            }
        }

        public async void Disconnect()
        {
            if (_disposed) return;

            try
            {
                // Shutdown the socket if it's connected
                if (_socket.Connected)
                {
                    await SocketWizard.Instance.BroadcastMessage($"Client {Name} disconnected.");
                    
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException ex)
            {
                HandleSocketException(ex);
            }
            catch (ObjectDisposedException)
            {
                // Socket is already disposed, nothing to do
            }
            finally
            {
                
                // Trigger the Disconnected event and dispose
                OnDisconnected();
                Dispose();
            }
        }

        private void OnDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private void HandleSocketException(SocketException ex)
        {
            Scribe.Error(ex, "SocketException occurred.");
            SocketError?.Invoke(this, ex);
        }

        private static void HandleGeneralException(Exception ex, string context)
        {
            Scribe.Error(ex, $"Unexpected error in {context}.");
        }

        public void Dispose()
        {
            if (_disposed) return;

            _socket.Close();
            _socket.Dispose();
            _disposed = true;

            GC.SuppressFinalize(this);
        }

        ~ClientSocket()
        {
            Dispose();
        }
    }
}
