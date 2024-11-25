using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Processes;
using OnlineGame.Core.Types;
using OnlineGame.Game.GameObjects.Player;
using OnlineGame.Network.Client;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Network
{
    /// <summary>
    /// The GateKeeper class manages the border of incoming new connections and protects the realm
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
        private static async Task ProcessClientAsync(Socket clientSocket)
        {
            ClientSocket client = new(clientSocket);
            await SocketWizard.Instance.Subscribe(client);

            try
            {
                Scribe.Scry($"New Client: {client.Name} from {client.GetSocketAddressDNS()}");

                // Greet and get username
                string? userName = await GreetAndGetUsername(client);
                if (string.IsNullOrEmpty(userName))
                {
                    client.Disconnect();
                    return;
                }

                userName = userName.ToLower();

                // Check if the playerFile file exists to determine if this is a new user
                string filePath = $@"{Constellations.PLAYERSTORAGE}{userName}.json";
                bool newUser = !CommunicationsOperator.FileExists(filePath);

                if (newUser)
                {
                    // Prompt the client to set up a password
                    string? newPassword = await SetupNewPlayerPassword(client, userName);

                    if (string.IsNullOrEmpty(newPassword))
                    {
                        client.Disconnect();
                        return;
                    }

                    // Create a new playerFile file
                    PlayerFile newPlayerFile = new()
                    {
                        UserName = userName,
                        PasswordHash = CommunicationsOperator.HashPassword(newPassword), // Hash the password
                        CreatedDate = DateTime.UtcNow
                    };

                    // Save the new playerFile file using the CommunicationsOperator
                    await CommunicationsOperator.SavePlayerFile(newPlayerFile);

                    await client.SendMessageLineAsync("Your account has been created successfully. Welcome!");
                    Scribe.Scry($"New player file created for {userName}.");
                }
                else
                {
                    // Existing user: Ask for their password
                    string? password = await AskForPassword(client);

                    if (password == null)
                    {
                        client.Disconnect();
                        return;
                    }

                    // Load the playerFile's file and verify the password
                    PlayerFile? playerFile = await CommunicationsOperator.LoadPlayerFile(userName);

                    if (playerFile == null || !PasswordHasher.VerifyPassword(password, playerFile.PasswordHash))
                    {
                        await client.SendMessageLineAsync("Invalid username or password. Disconnecting...");
                        client.Disconnect();
                        return;
                    }

                    await client.SendMessageLineAsync($"Welcome back, {userName}!");
                    Scribe.Scry($"{userName} has successfully logged in.");
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


        private static async Task<string?> GreetAndGetUsername(ClientSocket client)
        {
            return await InputValidationHelper.GetValidatedInputAsync(
                inputProvider: async () =>
                {
                    await client.SendMessageAsync("By what name are you known? ");
                    return UniversalTranslator.CleanTelnetInput(await client.ReceiveRawMessageAsync());
                },
                validationCondition: input => !string.IsNullOrWhiteSpace(input) && FilterWizard.Instance.IsValidUsername(input),
                maxRetries: 5
            );
        }

        private static async Task<string?> AskForPassword(ClientSocket client)
        {
            return await InputValidationHelper.GetValidatedInputAsync(
                inputProvider: async () =>
                {
                    await client.SendMessageAsync("Please provide the password: ");
                    return UniversalTranslator.CleanTelnetInput(await client.ReceiveRawMessageAsync());
                },
                validationCondition: input => !string.IsNullOrWhiteSpace(input) && FilterWizard.Instance.IsValidPassword(input),
                maxRetries: 5
            );
        }

        private static async Task<string?> SetupNewPlayerPassword(ClientSocket client, string userName)
        {
            return await InputValidationHelper.GetValidatedInputAsync(
                inputProvider: async () =>
                {
                    await client.SendMessageLineAsync($"Please provide a password for {userName}.");
                    await client.SendMessageLineAsync("Passwords can be up to 20 characters long and include any terminal character.");
                    await client.SendMessageAsync("Enter Password: ");
                    return UniversalTranslator.CleanTelnetInput(await client.ReceiveRawMessageAsync());
                },
                validationCondition: input => !string.IsNullOrWhiteSpace(input), // Adjust if you want stricter validation.
                maxRetries: 5
            );
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
