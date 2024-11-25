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
    /// The GateKeeper class manages client connections, processes incoming clients,
    /// and maintains the state of the subsystem.
    /// </summary>
    public sealed class GateKeeper : ISubsystem
    {
        /// <summary>
        /// Singleton instance of GateKeeper.
        /// </summary>
        private static readonly Lazy<GateKeeper> _instance = new(() => new GateKeeper());
        public static GateKeeper Instance => _instance.Value;

        private ThreadSafeList<Socket> socketsJoined = new();

        /// <summary>
        /// The name of the subsystem.
        /// </summary>
        public string Name => "GateKeeper";

        /// <summary>
        /// The current state of the subsystem.
        /// </summary>
        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        /// <summary>
        /// Event triggered when the subsystem state changes.
        /// </summary>
        public event EventHandler<SystemEventArgs>? StateChanged;

        /// <summary>
        /// Private constructor to enforce the singleton pattern.
        /// </summary>
        private GateKeeper() { }

        /// <summary>
        /// Starts the GateKeeper subsystem and transitions its state to Running.
        /// </summary>
        public void Start()
        {
            CurrentSystemState = SubsystemState.Running;
            RaiseStateChanged("GateKeeper started successfully.");
            socketsJoined = new();
        }

        /// <summary>
        /// Stops the GateKeeper subsystem and transitions its state to Stopped.
        /// </summary>
        public void Stop()
        {
            CurrentSystemState = SubsystemState.Stopped;
            RaiseStateChanged("GateKeeper stopped successfully.");
        }

        /// <summary>
        /// Processes an incoming client connection asynchronously.
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
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        /// <summary>
        /// Handles the logic for processing a client, including greeting, authentication, and setup.
        /// </summary>
        /// <param name="clientSocket">The client socket to process.</param>
        private static async Task ProcessClientAsync(Socket clientSocket)
        {
            ClientSocket client = new(clientSocket);
            await SocketWizard.Instance.Subscribe(client);

            try
            {
                Scribe.Scry($"New Client: {client.Name} from {client.GetSocketAddressDNS()}");

                await GreetNewUser(client);

                string? userName = UniversalTranslator.CleanTelnetInput(await client.ReceiveRawMessageAsync());
                int retries = 0;

                // Retry loop for valid username input
                while ((string.IsNullOrWhiteSpace(userName) || !FilterWizard.Instance.IsValidUsername(userName)) && retries < 5)
                {
                    await GreetNewUser(client);
                    userName = UniversalTranslator.CleanTelnetInput(await client.ReceiveRawMessageAsync());
                    retries++;
                }

                if (retries >= 5)
                {
                    await client.SendMessageLineAsync("Too many invalid username attempts. Disconnecting...");
                    client.Disconnect();
                    return;
                }

                await client.SendMessageLineAsync($"Welcome {userName}");
                string playerFilePath = $@"{Constellations.PLAYERSTORAGE}{userName.ToLower()}.txt";

                bool isNewPlayer = !CommunicationsOperator.FileExists(playerFilePath);

                if (isNewPlayer)
                {
                    Scribe.Scry("New player detected.");
                    await HandleNewPlayer(client, userName);
                }
                else
                {
                    await AskForPassword(client, userName.ToLower());
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Unhandled exception in ProcessClientAsync.");
            }
            finally
            {
                SocketWizard.Instance.Unsubscribe(client);
            }
        }

        /// <summary>
        /// Greets a new user and requests their name.
        /// </summary>
        /// <param name="client">The client socket.</param>
        private static async Task GreetNewUser(ClientSocket client)
        {
            await client.SendMessageLineAsync($"Welcome to {Constellations.GAMENAME}");
            await client.SendANSIMessageLineAsync($"You are connecting from {client.GetSocketAddressDNS()}");
            await client.SendMessageAsync($"By what name are you known? ");
        }

        /// <summary>
        /// Prompts the client for a password.
        /// </summary>
        /// <param name="client">The client socket.</param>
        /// <param name="userName">The username of the client.</param>
        private static async Task AskForPassword(ClientSocket client, string userName)
        {
            await client.SendANSIMessageAsync($"Please provide the password for %^RED%^{userName}%^RESET%^: ");
        }

        /// <summary>
        /// Prompts a new player to set a password and creates their account.
        /// </summary>
        /// <param name="client">The client socket.</param>
        /// <param name="userName">The username of the new player.</param>
        private static async Task HandleNewPlayer(ClientSocket client, string userName)
        {
            int retries = 0;
            string password = string.Empty;

            while (string.IsNullOrWhiteSpace(password) && retries < 5)
            {
                if (retries > 0)
                {
                    await client.SendMessageLineAsync($"Attempt {retries}/5: Invalid password. Please try again.");
                }

                await AskNewPlayerPassword(client, userName);
                password = UniversalTranslator.CleanTelnetInput(await client.ReceiveRawMessageAsync());
                retries++;
            }

            if (retries >= 5)
            {
                await client.SendMessageLineAsync("Too many invalid password attempts. Disconnecting...");
                client.Disconnect();
                return;
            }

            try
            {
                await CommunicationsOperator.CreateNewPlayerFile(userName.ToLower(), password);
                await client.SendMessageLineAsync("Your account has been created successfully.");
                Scribe.Scry($"New player file created for {userName}.");
            }
            catch (Exception ex)
            {
                await client.SendMessageLineAsync("An error occurred while creating your account. Please try again later.");
                Scribe.Error(ex, $"Failed to create player file for {userName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Prompts a new player to provide a password.
        /// </summary>
        /// <param name="client">The client socket.</param>
        /// <param name="userName">The username of the new player.</param>
        private static async Task AskNewPlayerPassword(ClientSocket client, string userName)
        {
            await client.SendMessageLineAsync($"Please provide a password for {userName}");
            await client.SendMessageLineAsync($"Passwords can be up to 20 characters long and include any terminal character.");
            await client.SendMessageAsync($"Enter Password: ");
        }

        /// <summary>
        /// Raises the StateChanged event and logs the state transition.
        /// </summary>
        /// <param name="message">A message describing the state change.</param>
        private void RaiseStateChanged(string message)
        {
            StateChanged?.Invoke(this, new SystemEventArgs("StateChange", Name, message));
            Scribe.Notification(message);
        }
    }
}
