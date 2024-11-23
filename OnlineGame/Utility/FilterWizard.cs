using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Types;
using OnlineGame.Utility.Types;

namespace OnlineGame.Utility
{
    /// <summary>
    /// The FilterWizard class manages filtering operations for banned words and validates usernames.
    /// Implements ISubsystem for monitoring and lifecycle management.
    /// </summary>
    public sealed class FilterWizard : ISubsystem
    {
        // Singleton instance
        private static readonly Lazy<FilterWizard> _instance = new(() => new FilterWizard());
        public static FilterWizard Instance => _instance.Value;

        /// <summary>
        /// The name of the subsystem.
        /// </summary>
        public string Name => "FilterWizard";

        /// <summary>
        /// Current state of the subsystem.
        /// </summary>
        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        /// <summary>
        /// Event triggered when the subsystem state changes.
        /// </summary>
        public event EventHandler<SystemEventArgs>? StateChanged;

        /// <summary>
        /// Thread-safe list of banned words.
        /// </summary>
        public ThreadSafeList<string> BannedWords { get; private set; } = new();

        // Private constructor for singleton
        private FilterWizard() { }

        /// <summary>
        /// Starts the FilterWizard subsystem.
        /// </summary>
        public void Start()
        {
            CurrentSystemState = SubsystemState.Running;
            RaiseStateChanged("FilterWizard started successfully.");
        }

        /// <summary>
        /// Stops the FilterWizard subsystem.
        /// </summary>
        public void Stop()
        {
            CurrentSystemState = SubsystemState.Stopped;
            RaiseStateChanged("FilterWizard stopped successfully.");
        }

        /// <summary>
        /// Filters out banned words from a given input.
        /// Replaces banned words with asterisks (*).
        /// </summary>
        /// <param name="input">The string to filter.</param>
        /// <returns>A filtered string.</returns>
        public string FilterString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            string result = input;

            foreach (string bannedWord in BannedWords.ToList())
            {
                string replacement = new('*', bannedWord.Length);
                result = result.Replace(bannedWord, replacement, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Validates if the username meets the required criteria.
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <returns>True if valid, otherwise false.</returns>
        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length > 14)
                return false;

            // Check for non-alphabetical characters or whitespace
            if (!username.All(char.IsLetter))
                return false;

            return true;
        }

        /// <summary>
        /// Formats the username to have proper casing.
        /// Ensures the first letter is capitalized.
        /// </summary>
        /// <param name="username">The username to format.</param>
        /// <returns>Formatted username.</returns>
        public static string FormatUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return username;

            return char.ToUpper(username[0]) + username[1..].ToString().ToLower();
        }

        /// <summary>
        /// Checks if a name is banned.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if banned, otherwise false.</returns>
        public bool IsNameBanned(string name)
        {
            return BannedWords.Contains(name);
        }

        /// <summary>
        /// Adds a new name to the list of banned words.
        /// </summary>
        /// <param name="name">The name to add.</param>
        public void AddBannedName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !IsNameBanned(name))
            {
                BannedWords.Add(name);
            }
        }

        /// <summary>
        /// Removes a name from the list of banned words.
        /// </summary>
        /// <param name="name">The name to remove.</param>
        public void RemoveBannedName(string name)
        {
            BannedWords.Remove(name);
        }

        /// <summary>
        /// Loads banned words from a CSV file.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        public void LoadBannedWordsFromCsv(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Banned words file not found at: {filePath}");

            var words = File.ReadAllLines(filePath)
                            .Select(line => line.Trim())
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .Distinct(StringComparer.OrdinalIgnoreCase);

            BannedWords.Clear();
            BannedWords.AddRange(words);
        }

        /// <summary>
        /// Saves the current list of banned words to a CSV file.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        public void SaveBannedWordsToCsv(string filePath)
        {
            File.WriteAllLines(filePath, BannedWords.ToList());
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
