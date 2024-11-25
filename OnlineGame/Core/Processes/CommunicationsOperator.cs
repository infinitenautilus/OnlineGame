using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Types;
using OnlineGame.Utility;
using OnlineGame.Utility.Types;

namespace OnlineGame.Core.Processes
{
    /// <summary>
    /// The purpose of this singleton is to act as a layer that writes and reads from the disk for you,
    /// and operates as a subsystem.
    /// </summary>
    public class CommunicationsOperator : ISubsystem
    {
        private static readonly Lazy<CommunicationsOperator> _instance = new(() => new CommunicationsOperator());
        public static CommunicationsOperator Instance => _instance.Value;

        private SubsystemState _currentSystemState = SubsystemState.Stopped;

        private CommunicationsOperator()
        {
            Name = "CommunicationsOperator";
        }

        public string Name { get; }

        public SubsystemState CurrentSystemState
        {
            get => _currentSystemState;
            private set
            {
                if (_currentSystemState != value)
                {
                    _currentSystemState = value;
                    OnStateChanged(new SystemEventArgs(Name, value));
                }
            }
        }

        public event EventHandler<SystemEventArgs>? StateChanged;

        protected virtual void OnStateChanged(SystemEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        public void Start()
        {
            if (CurrentSystemState == SubsystemState.Running)
                return;

            Console.WriteLine($"{Name} is starting...");
            CurrentSystemState = SubsystemState.Running;
        }

        public void Stop()
        {
            if (CurrentSystemState == SubsystemState.Stopped)
                return;

            Console.WriteLine($"{Name} is stopping...");
            CurrentSystemState = SubsystemState.Stopped;
        }

        public static bool FileExists(string fileName)
        {
            return File.Exists(fileName);
        }

        /// <summary>
        /// Reads the contents of a file asynchronously and returns it as a raw string.
        /// </summary>
        public static async Task<string> RawReadAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            using var reader = new StreamReader(filePath, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Writes the given string content to a file asynchronously.
        /// </summary>
        public static async Task RawWriteAsync(string filePath, string content)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            await writer.WriteAsync(content);
        }

        /// <summary>
        /// Reads a CSV file asynchronously and parses its contents into a string array or ThreadSafeList.
        /// </summary>
        public static async Task<object> CSVReadAsync(string filePath, bool useThreadSafeList = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var lines = new List<string>();
            using var reader = new StreamReader(filePath, Encoding.UTF8);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line != null)
                    lines.Add(line);
            }

            var parsedLines = lines.Select(line => line.Split(',').Select(value => value.Trim()).ToArray());

            if (useThreadSafeList)
            {
                var threadSafeList = new ThreadSafeList<string>();
                foreach (var line in parsedLines)
                {
                    threadSafeList.Add(string.Join(",", line));
                }
                return threadSafeList;
            }

            return parsedLines.Select(line => string.Join(",", line)).ToArray();
        }

        /// <summary>
        /// Writes the given string array or ThreadSafeList to a CSV file asynchronously.
        /// </summary>
        public static async Task WriteCsvAsync(string filePath, IEnumerable<string[]> csvData)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            using StreamWriter writer = new(filePath, false, Encoding.UTF8);

            foreach (var line in csvData)
            {
                await writer.WriteLineAsync(string.Join(",", line));
            }

            writer.Close();
        }

        public static Task CreateNewPlayerFile(string userName, string password)
        {
            return CreateNewPlayerFile(userName, password, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        public static async Task CreateNewPlayerFile(string userName, string password, System.Text.Json.JsonSerializerOptions options)
        {
            string encryptedPassword = EncryptPassword(password);
            var playerData = new
            {
                UserName = userName,
                Password = encryptedPassword
            };

            string playerFilePath = $"{Constellations.PLAYERSTORAGE}{userName}.txt";
            
            Scribe.Scry($"playerFilePath: {playerFilePath}");

            string jsonData = System.Text.Json.JsonSerializer.Serialize(playerData,
                options: options);

            await File.WriteAllTextAsync(playerFilePath, jsonData);
        }

        public static async Task<bool> LoadPlayerFromFile(string userName, string password)
        {
            try
            {
                // Build the file path
                string playerFilePath = $"{Constellations.PLAYERSTORAGE}{userName.ToLower().Trim()}.json";

                // Check if the file exists
                if (!File.Exists(playerFilePath))
                {
                    Scribe.Scry($"Player file not found for {userName}.");
                    return false;
                }

                // Read the JSON file
                string jsonData = await File.ReadAllTextAsync(playerFilePath, Encoding.UTF8);

                // Deserialize the JSON to extract the stored password
                var playerData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);

                if (playerData == null || !playerData.TryGetValue("Password", out string? storedPassword))
                {
                    Scribe.Scry($"Malformed player file for {userName}.");
                    return false;
                }

                // Validate the provided password against the stored password
                return ValidatePassword(password, storedPassword);
            }
            catch (Exception ex)
            {
                Scribe.Error(ex, $"Error loading player file for {userName}: {ex.Message}");
                return false;
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
            if (string.IsNullOrWhiteSpace(inputPassword))
                return false;

            return EncryptPassword(inputPassword) == storedPassword;
        }



    }
}
