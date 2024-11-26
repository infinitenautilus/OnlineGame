using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using OnlineGame.Config;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Processes;
using OnlineGame.Core.Types;
using OnlineGame.Game.Core.Processes;
using OnlineGame.Game.GameObjects.Things.Living.Player;
using OnlineGame.Game.Interfaces;
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

        private ThreadSafeList<Socket> socketsJoined = [];

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
            socketsJoined = [];
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
                string? userName = await RequestUsername(client);

                if(userName == null)
                {
                    return;
                }

                bool newUser = !await PlayerService.UserNameExistsAsync(userName);

                string password = await RequestPassword(newUser, userName, client);

                PlayerObject tempPlayer = await PlayerService.InitializeNewPlayer(userName, password);
                
                tempPlayer.CreatedDate = DateTime.Now;

                await tempPlayer.SendMessageAsync("Welcome to the MUD.");

                Scribe.Scry($"Player loaded: {tempPlayer.Name}");

                await PlayerService.SavePlayerAsync(tempPlayer);
                
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, "Unhandled exception in ProcessClientAsync.");
            }
            finally
            {
                // SocketWizard.Instance.Unsubscribe(client);
            }
        }

        private static async Task<string?> RequestUsername(ClientSocket client)
        {
            try
            {
                Scribe.Scry($"New Client: {client.Name} from {client.GetSocketAddressDNS()}");

                // Greet and get username
                string? userName = await GreetAndGetUsername(client);
                if (string.IsNullOrEmpty(userName))
                {
                    client.Disconnect();
                    return null;
                }

                userName = userName.ToLower();
                
                return userName;
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
                return null;
            }
            
        }

        private static async Task<string> RequestPassword(bool newUser, string userName, ClientSocket client)
        {
            try
            {
                if (newUser)
                {
                    // Prompt the client to set up a password
                    string? newPassword = await SetupNewPlayerPassword(client, userName);

                    if (string.IsNullOrEmpty(newPassword))
                    {
                        client.Disconnect();
                        return string.Empty;
                    }

                    return newPassword;
                }
                else
                {
                    // Existing user: Ask for their password once
                    string? password = await AskForPassword(client);

                    if (password == null)
                    {
                        client.Disconnect();
                        return string.Empty;
                    }

                    // Validate password
                    PlayerObject? player = await PlayerService.LoadPlayerAsync(userName);

                    if (player == null || !CommunicationsOperator.VerifyPassword(password, player.PasswordHash))
                    {
                        await client.WriteLineAsync("Invalid password. Disconnecting.");
                        client.Disconnect();
                        return string.Empty;
                    }

                    return password;
                }
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
                return string.Empty;
            }
        }

        private static async Task<string?> GreetAndGetUsername(ClientSocket client)
        {
            return await InputValidationHelper.GetValidatedInputAsync(
                inputProvider: async () =>
                {
                    await client.WriteAsync("By what name are you known? ");
                    return await client.ReceiveAsync();
                },
                validationCondition: input => !string.IsNullOrWhiteSpace(input) && CommunicationsOperator.IsValidUsername(input),
                maxRetries: 5
            );
        }

        private static async Task<string?> AskForPassword(ClientSocket client)
        {
            return await InputValidationHelper.GetValidatedInputAsync(
                inputProvider: async () =>
                {
                    await client.WriteAsync("Please provide the password: ");
                    return await client.ReceiveAsync();
                },
                validationCondition: input => CommunicationsOperator.IsValidPassword(input),
                maxRetries: 3
            );
        }

        private static async Task<string?> SetupNewPlayerPassword(ClientSocket client, string userName)
        {
            return await InputValidationHelper.GetValidatedInputAsync(
                inputProvider: async () =>
                {
                    await client.WriteLineAsync($"Please provide a password for {userName}.");
                    await client.WriteLineAsync("Passwords must be between 5 and 20 characters long and include any terminal character.");
                    await client.WriteAsync("Enter Password: ");


                    return await client.ReceiveAsync();
                },

                validationCondition: input => CommunicationsOperator.IsValidPassword(input),
                maxRetries: 3
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
