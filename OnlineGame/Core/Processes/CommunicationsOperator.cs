using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Config;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Types;
using OnlineGame.Game.GameObjects.Things.Living.Player;
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

        /// <summary>
        /// Initializes a new instance of the CommunicationsOperator class.
        /// </summary>
        private CommunicationsOperator()
        {
            Name = "CommunicationsOperator";
        }

        /// <summary>
        /// Gets the name of the subsystem.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the current state of the subsystem.
        /// Triggers the StateChanged event when the state changes.
        /// </summary>
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

        /// <summary>
        /// Event triggered when the subsystem state changes.
        /// </summary>
        public event EventHandler<SystemEventArgs>? StateChanged;

        /// <summary>
        /// Raises the StateChanged event.
        /// </summary>
        /// <param name="e">Event data containing state change details.</param>
        protected virtual void OnStateChanged(SystemEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Starts the CommunicationsOperator subsystem.
        /// </summary>
        public void Start()
        {
            if (CurrentSystemState == SubsystemState.Running)
                return;

            Console.WriteLine($"{Name} is starting...");
            CurrentSystemState = SubsystemState.Running;
        }

        /// <summary>
        /// Stops the CommunicationsOperator subsystem.
        /// </summary>
        public void Stop()
        {
            if (CurrentSystemState == SubsystemState.Stopped)
                return;

            Console.WriteLine($"{Name} is stopping...");
            CurrentSystemState = SubsystemState.Stopped;
        }

        /// <summary>
        /// Checks whether a file exists at the specified path.
        /// </summary>
        /// <param name="fileName">The path of the file to check.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public static bool FileExists(string fileName)
        {
            return File.Exists(fileName);
        }

        /// <summary>
        /// Reads the contents of a file asynchronously and returns it as a raw string.
        /// </summary>
        /// <param name="filePath">The path of the file to read.</param>
        /// <returns>A task that represents the asynchronous operation, containing the file contents as a string.</returns>
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
        /// <param name="filePath">The path of the file to write to.</param>
        /// <param name="content">The content to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static async Task RawWriteAsync(string filePath, string content)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            await writer.WriteAsync(content);
        }

        /// <summary>
        /// Reads a CSV file asynchronously and parses its contents.
        /// </summary>
        /// <param name="filePath">The path of the CSV file.</param>
        /// <param name="useThreadSafeList">Whether to use a ThreadSafeList for storage.</param>
        /// <returns>A task that represents the asynchronous operation, returning the parsed CSV data.</returns>
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
        /// <param name="filePath">The path of the CSV file to write to.</param>
        /// <param name="csvData">The CSV data to write.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
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

        public static string HashPassword(string password)
        {
            return EncryptPassword(password);
        }

        /// <summary>
        /// Encrypts a password for storage.
        /// </summary>
        /// <param name="password">The plaintext password to encrypt.</param>
        /// <returns>The encrypted password.</returns>
        private static string EncryptPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
