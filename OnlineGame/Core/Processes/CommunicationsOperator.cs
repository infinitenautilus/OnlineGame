using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    public static class CommunicationsOperator
    {
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

            using StreamReader reader = new(filePath, Encoding.UTF8);
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

            List<string> lines = [];

            using StreamReader reader = new(filePath, Encoding.ASCII);
            
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (line != null)
                    lines.Add(line);
            }

            var parsedLines = lines.Select(line => line.Split(',').Select(value => value.Trim()).ToArray());

            if (useThreadSafeList)
            {
                ThreadSafeList<string> threadSafeList = [];

                foreach (string[] line in parsedLines)
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

        // Hash password using a hashing algorithm (e.g., SHA256 for demonstration)
        public static string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            
            return Convert.ToBase64String(hashedBytes);
        }

        // Compare a hashed password against a plain text password
        public static bool VerifyPassword(string enteredPassword, string storedHashedPassword)
        {
            string hashedPassword = HashPassword(enteredPassword);
            return hashedPassword == storedHashedPassword;
        }

        public static bool IsValidPassword(string? password)
        {
            // Check if password is null or empty
            if (string.IsNullOrEmpty(password))
                return false;

            // Check if password length is between 5 and 20 characters
            if (password.Length < 5 || password.Length > 20)
                return false;

            // Check if all characters in the password are on a standard keyboard
            // Standard ASCII printable characters range from 32 (space) to 126 (~)
            return password.All(c => c >= 32 && c <= 126);
        }


        /// <summary>
        /// Validates if the username meets the required criteria.
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <returns>True if valid, otherwise false.</returns>
        public static bool IsValidUsername(string username)
        {
            // Check for null, empty, or whitespace, and maximum length constraint
            if (string.IsNullOrWhiteSpace(username) || username.Length > 14)
                return false;

            // Check if the name is banned
            if (FilterWizard.Instance.IsNameBanned(username))
                return false;

            // Check if all characters are lowercase alphabetical
            if (!username.All(c => c >= 'a' && c <= 'z'))
                return false;

            return true;
        }

    }
}
