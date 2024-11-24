using System;
using System.IO;
using System.Threading;

namespace OnlineGame.Utility
{
    public static class Scribe
    {
        private static TextWriter? _logFileWriter; // Optional file writer
        private static readonly object _writeLock = new(); // For thread-safe operations
        private static int _isShuttingDown = 0;

        /// <summary>
        /// Indicates if the application is shutting down.
        /// </summary>
        public static bool IsShuttingDown => Interlocked.CompareExchange(ref _isShuttingDown, 0, 0) == 1;

        /// <summary>
        /// Initializes the Scribe logging system with an optional file writer.
        /// </summary>
        /// <param name="logFilePath">Path to the log file. If null, no file logging is performed.</param>
        public static void Initialize(string? logFilePath = null)
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                _logFileWriter = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
            }
        }

        /// <summary>
        /// Logs a general message with gray console output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Scry(string message)
        {
            LogMessage(message);
        }

        /// <summary>
        /// Logs a notification with yellow console output.
        /// </summary>
        /// <param name="message">The notification message to log.</param>
        public static void Notification(string message)
        {
            LogNotificationMessage(message);
        }

        /// <summary>
        /// Logs an error message with red console output.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void Error(Exception ex, string message)
        {
            LogErrorMessage("-*EXCEPTION ENCOUNTER*-");
            LogErrorMessage($"Message; {ex.Message}");
            LogErrorMessage($"StackTrace: {ex.StackTrace}");
            LogErrorMessage($"EXCEPTION CUSTOM MESSAGE: {message}");
        }

        public static void Error(Exception ex)
        {
            LogErrorMessage("-*EXCEPTION ENCOUNTER*-");
            LogErrorMessage($"Message; {ex.Message}");
            LogErrorMessage($"StackTrace: {ex.StackTrace}");
        }

        /// <summary>
        /// Begins the shutdown sequence and logs a shutdown message.
        /// </summary>
        public static void BeginShutdown()
        {
            if (Interlocked.Exchange(ref _isShuttingDown, 1) == 0)
            {
                Scry("Application shutdown initiated.");
            }
        }

        /// <summary>
        /// Closes the log file writer, if one is in use.
        /// </summary>
        public static void Close()
        {
            if (_logFileWriter == null) return;

            lock (_writeLock)
            {
                try
                {
                    _logFileWriter.Flush();
                    _logFileWriter.Close();
                    _logFileWriter.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to close log file writer: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Formats and logs a message to the console and optionally to a log file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color for console output.</param>
        private static void LogMessage(string message)
        {
            Safelog(TimeStampMessage(message), ConsoleColor.White);
        }

        private static void LogNotificationMessage(string message)
        {
            Safelog(TimeStampMessage(message), ConsoleColor.Yellow);            
        }

        private static void LogErrorMessage(string message)
        {
            Safelog(TimeStampMessage(message), ConsoleColor.Red);
        }

        private static void Safelog(string message, ConsoleColor consoleColor)
        {
            if (IsShuttingDown) return;

            try
            {
                lock (_writeLock)
                {
                    Console.ForegroundColor = consoleColor;
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.White;

                    // Log to the file if enabled
                    _logFileWriter?.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION ENCOUNTERED IN SCRIBE SAFELOG\b{ex.Message}\n{ex.StackTrace}");
            }
        }

        private static string TimeStampMessage(string message)
        {
            return $"[ {Constellations.TIMESTAMP} ] - [{message}] ";
        }
    }
}
