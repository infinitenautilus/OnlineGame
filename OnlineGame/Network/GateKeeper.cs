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
            
            await client.SendMessageLineAsync($"Cured Username: {curedUserName}");

            bool isNewPlayer = !CommunicationsOperator.FileExists($"{Constellations.PLAYERSTORAGE}{curedUserName}.txt");

            if (isNewPlayer)
            {
                Scribe.Scry("NewPlayer detected.");

                string password = string.Empty;

                retries = 0;

                while (string.IsNullOrWhiteSpace(password) && retries < 5)
                {
                    if (retries != 0)
                    {
                        await client.SendMessageLineAsync("Invalid password. Please try again.");
                    }

                    await AskNewPlayerPassword(client, curedUserName);

                    password = await client.ReceiveMessageAsync();

                    retries++;
                }

                if (!string.IsNullOrWhiteSpace(password))
                {
                    try
                    {
                        await CommunicationsOperator.CreateNewPlayerFile(curedUserName, password);
                        await client.SendMessageLineAsync("Your account has been created successfully.");
                        Scribe.Scry($"New player file created for {userName}.");
                    }
                    catch (Exception ex)
                    {
                        await client.SendMessageLineAsync("An error occurred while creating your account. Please try again later.");
                        Scribe.Error(ex, $"Failed to create player file for {userName}: {ex.Message}");
                    }
                }
                else
                {
                    await client.SendMessageLineAsync("Failed to create an account. Too many invalid attempts.");
                    Scribe.Scry($"New player registration failed for {userName}.");
                    return;
                }
            }
            else
            {
               await AskForPassword(client, curedUserName);
            }
            //SocketWizard.Instance.Unsubscribe(client);
        }

        private static async Task GreetNewUser(ClientSocket client)
        {
            await client.SendMessageLineAsync($"Welcome to {Constellations.GAMENAME}");
            await client.SendANSIMessageAsync($"You are connecting from %^RED%^{client.GetSocketAddressDNS()}%^RESET%^");
            await client.SendMessageAsync($"By what name are you known? ");
        }
        
        private static async Task AskForPassword(ClientSocket client, string userName)
        {
            await client.SendANSIMessageAsync($"Please provide the password for %^RED%^{userName}%^RESET%^: ");
        }

        private static async Task AskNewPlayerPassword(ClientSocket client, string userName)
        {
            await client.SendMessageLineAsync($"Please provide a password for {userName}");
            await client.SendMessageLineAsync($"Passwords can be up to 20 characters long, and include any terminal character.");
            await client.SendMessageAsync($"Enter Password: ");
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
