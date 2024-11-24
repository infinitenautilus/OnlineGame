using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineGame.Utility
{
    public static class Scribe
    {
        private static readonly StreamWriter? _writer = new(Constellations.LOGFILE, true) { AutoFlush = true };
        private static int _isShuttingDown = 0; // Use an atomic integer for thread-safe shutdown flag.
        private static readonly object _writeLock = new(); // Lock object for writer synchronization.

        public static bool IsShuttingDown => Interlocked.CompareExchange(ref _isShuttingDown, 0, 0) == 1;

        /// <summary>
        /// Logs a standard message with gray console output.
        /// </summary>
        public static void Scry(string message)
        {
            LogToConsole(message, ConsoleColor.Gray);
            WriteMessage(message);
        }

        /// <summary>
        /// Logs a notification with yellow console output.
        /// </summary>
        public static void Notification(string message)
        {
            LogToConsole(message, ConsoleColor.Yellow);
            WriteMessage(message);
        }

        /// <summary>
        /// Handles SocketException and logs relevant details.
        /// </summary>
        public static void HandleSocketException(SocketException ex)
        {
            Error(ex, "SocketException occurred.");
        }

        /// <summary>
        /// Handles a general exception with a specific context message.
        /// </summary>
        public static void HandleGeneralException(Exception ex, string context = "")
        {
            Error(ex, string.IsNullOrEmpty(context) ? $"Unexpected error: {ex.Message}" : $"Unexpected error in {context}");
        }

        /// <summary>
        /// Logs an exception with optional custom message.
        /// </summary>
        public static void Error(Exception ex, string customMessage = "")
        {
            LogToConsole(customMessage, ConsoleColor.Red);
            LogExceptionDetails(ex, customMessage);
        }

        /// <summary>
        /// Begins the shutdown sequence and logs the event.
        /// </summary>
        public static void BeginShutdown()
        {
            if (Interlocked.Exchange(ref _isShuttingDown, 1) == 0)
            {
                Scry("Application shutdown initiated.");
            }
        }

        /// <summary>
        /// Flushes, closes, and disposes of the writer safely.
        /// </summary>
        public static void CloseWriter()
        {
            if (_writer != null)
            {
                lock (_writeLock)
                {
                    try
                    {
                        if (_writer.BaseStream.CanWrite)
                        {
                            _writer.Flush();
                        }
                        Scry("Scribe: Writer flushed successfully.");
                        _writer.Close();
                        Scry("Scribe: Writer closed successfully.");
                        _writer.Dispose();
                        Scry("Scribe: Writer disposed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing writer: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                }
            }
        }

        /// <summary>
        /// Writes a log message to the writer with caller information.
        /// </summary>
        private static void WriteMessage(
            string message,
            [System.Runtime.CompilerServices.CallerMemberName] string callerName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int callerLineNumber = 0)
        {
            if (IsShuttingDown)
                return;

            string className = Path.GetFileNameWithoutExtension(callerFilePath);
            string formattedMessage = $"[{Constellations.TIMESTAMP}] {message} [from] {className}.{callerName}, Line: {callerLineNumber}";

            lock (_writeLock)
            {
                Console.WriteLine(formattedMessage);
                _writer?.WriteLine(formattedMessage);
            }
        }

        /// <summary>
        /// Logs a message to the console with a specific color.
        /// </summary>
        private static void LogToConsole(string message, ConsoleColor color)
        {
            lock (_writeLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logs detailed exception information.
        /// </summary>
        private static void LogExceptionDetails(Exception ex, string customMessage)
        {
            string shutdownInfo = IsShuttingDown ? " (During Shutdown)" : "";
            WriteMessage($"EXCEPTION ENCOUNTERED{shutdownInfo}{(string.IsNullOrEmpty(customMessage) ? "" : $": {customMessage}")}");
            WriteMessage($"Message: {ex.Message}");
            WriteMessage($"StackTrace: {ex.StackTrace}");
            WriteMessage($"Source: {ex.Source}");
        }
    }
}
