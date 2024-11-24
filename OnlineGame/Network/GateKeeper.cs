using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Processes;
using OnlineGame.Core.Types;
using OnlineGame.Network.Client;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Network
{
    /// <summary>
    /// The GateKeeper class acts as a subsystem responsible for managing client connections.
    /// It processes incoming clients and maintains subsystem state.
    /// </summary>
    public sealed class GateKeeper : ISubsystem
    {
        // Singleton instance of GateKeeper
        private static readonly Lazy<GateKeeper> _instance = new(() => new GateKeeper());
        public static GateKeeper Instance => _instance.Value;

        private ThreadSafeList<Socket> socketsJoined = new();

        /// <summary>
        /// Gets the name of the subsystem.
        /// </summary>
        public string Name => "GateKeeper";

        /// <summary>
        /// Current state of the subsystem.
        /// </summary>
        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        /// <summary>
        /// Event triggered when the subsystem state changes.
        /// </summary>
        public event EventHandler<SystemEventArgs>? StateChanged;

        // Private constructor to enforce singleton pattern
        private GateKeeper() { }

        /// <summary>
        /// Starts the GateKeeper subsystem and transitions it to a Running state.
        /// </summary>
        public void Start()
        {
            CurrentSystemState = SubsystemState.Running;
            RaiseStateChanged("GateKeeper started successfully.");
            socketsJoined = new();
        }


        /// <summary>
        /// Stops the GateKeeper subsystem and transitions it to a Stopped state.
        /// </summary>
        public void Stop()
        {
            CurrentSystemState = SubsystemState.Stopped;
            RaiseStateChanged("GateKeeper stopped successfully.");
        }

        /// <summary>
        /// Initiates asynchronous processing of an incoming client connection.
        /// </summary>
        /// <param name="clientSocket">The connected client socket.</param>
        public async void HandleClient(Socket clientSocket)
        {
            socketsJoined.Add(clientSocket);

            try
            {
                await ProcessClientAsync(clientSocket);

                Scribe.Scry("Reached end of game loop for clientSocket.");
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        private static async Task ProcessClientAsync(Socket clientSocket)
        {
            ClientSocket client = new(clientSocket);
            await SocketWizard.Instance.Subscribe(client);

            Scribe.Scry($"New Client: {client.Name} from {client.GetSocketAddressDNS()}");

            await GreetNewUser(client);

            string? userName = await client.ReceiveMessageAsync();

            int retries = 0;

            while ((string.IsNullOrEmpty(userName) || FilterWizard.Instance.IsValidUsername(userName)) && retries < 5 )
            {
                await client.SendClearScreenAsync();

                await GreetNewUser(client);

                userName = await client.ReceiveMessageAsync();
                retries++;
            }

            await client.SendMessageLineAsync($"Welcome {userName}");

            string curedUserName = userName.ToLower().Trim();

            bool isNewPlayer = !File.Exists($"{Constellations.PLAYERSTORAGE}{curedUserName}.txt");

            if(isNewPlayer)
            {
                Scribe.Scry("NewPlayer detected.");
                await client.SendMessageLineAsync("Welcome new player!");

            }       
        
            SocketWizard.Instance.Unsubscribe(client);
        }

        private static async Task GreetNewUser(ClientSocket client)
        {
            await client.SendMessageLineAsync($"Welcome to {Constellations.GAMENAME}");
            await client.SendMessageLineAsync($"You are connecting from {client.GetSocketAddressDNS()}");
            await client.SendMessageAsync($"By what name are you known? ");
        }

        /// <summary>
        /// Encrypts a password for storage (simple hash for demonstration purposes).
        /// </summary>
        /// <param name="password">The plaintext password to encrypt.</param>
        /// <returns>The encrypted password.</returns>
        private static string EncryptPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Validates a plaintext password against an encrypted one.
        /// </summary>
        /// <param name="inputPassword">The plaintext password input.</param>
        /// <param name="storedPassword">The encrypted password stored on file.</param>
        /// <returns>True if the password is valid; otherwise, false.</returns>
        private static bool ValidatePassword(string? inputPassword, string storedPassword)
        {
            if (string.IsNullOrWhiteSpace(inputPassword)) 
                return false;
            
            return EncryptPassword(inputPassword) == storedPassword;
        }



        /// <summary>
        /// Raises the StateChanged event and logs the state change.
        /// </summary>
        /// <param name="message">The message to include with the state change event.</param>
        private void RaiseStateChanged(string message)
        {
            StateChanged?.Invoke(this, new SystemEventArgs("StateChange", Name, message));
            Scribe.Notification(message);
        }
    }
}
