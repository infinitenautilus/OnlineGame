using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
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
        public void HandleClient(Socket clientSocket)
        {
            socketsJoined.Add(clientSocket);

            _ = ProcessClientAsync(clientSocket);
            
        }

        private static async Task ProcessClientAsync(Socket clientSocket)
        {
            var client = new ClientSocket(clientSocket);

            try
            {
                // Log incoming connection
                Scribe.Scry($"Incoming client: {client.Name}");

                // Send welcome message
                await client.SendMessageAsync("Welcome to the Game");

                // Ask for character name
                await client.SendMessageAsync("Please enter your character name:");
                string? characterName = await client.ReceiveMessageAsync();

                // Validate character name
                while (string.IsNullOrWhiteSpace(characterName))
                {
                    await client.SendMessageAsync("Invalid character name. Please try again:");
                    characterName = await client.ReceiveMessageAsync();
                }

                // Check if the player exists
                string playerFilePath = Path.Combine(Constellations.PLAYERSTORAGE, $"{characterName}.txt");
                bool isNewPlayer = !File.Exists(playerFilePath);

                if (isNewPlayer)
                {
                    // New player creation
                    await client.SendMessageAsync("Character not found. Would you like to create a new one? (yes/no)");
                    string? response = await client.ReceiveMessageAsync();

                    if (response?.Trim().ToLower() != "yes")
                    {
                        await client.SendMessageAsync("Connection closed. Goodbye!");
                        return; // Exit the method
                    }

                    // Prompt the user to set a password
                    await client.SendMessageAsync("Please set a password for your new character:");
                    string? password = await client.ReceiveMessageAsync();

                    while (string.IsNullOrWhiteSpace(password))
                    {
                        await client.SendMessageAsync("Invalid password. Please try again:");
                        password = await client.ReceiveMessageAsync();
                    }

                    // Encrypt and save the new character's password
                    string encryptedPassword = EncryptPassword(password);
                    File.WriteAllText(playerFilePath, encryptedPassword);
                    await client.SendMessageAsync($"Character '{characterName}' created successfully!");
                }
                else
                {
                    // Existing player login
                    await client.SendMessageAsync("Please enter your password:");
                    string? password = await client.ReceiveMessageAsync();

                    while (!ValidatePassword(password, File.ReadAllText(playerFilePath)))
                    {
                        await client.SendMessageAsync("Invalid password. Please try again:");
                        password = await client.ReceiveMessageAsync();
                    }

                    await client.SendMessageAsync($"Welcome back, {characterName}!");
                }

                // (Commented for now) Pass control to Shroud for world generation
                // Shroud.LoadCharacter(characterName);

                // Temporary placeholder for main communication loop
                await client.SendMessageAsync("You are now in the game world. (Placeholder)");
            }
            catch (Exception ex)
            {
                // Handle any exceptions during client communication
                Scribe.Error(ex, "Error processing client.");
            }
            finally
            {
                // Ensure the client is properly disconnected
                client.Disconnect();
                Scribe.Scry($"Client {client.Name} disconnected.");
            }
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
            if (string.IsNullOrWhiteSpace(inputPassword)) return false;
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
