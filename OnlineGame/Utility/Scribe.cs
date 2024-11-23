using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame.Utility
{
    public static class Scribe
    {
        private readonly static StreamWriter? _writer = new(Constellations.LOGFILE, true) { AutoFlush = true };

        private static bool _isShuttingDown = false;

        public static void Scry(string message)
        {
            WriteMessage(message);
        }

        public static void Notification(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            WriteMessage(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void HandleSocketException(SocketException ex)
        {
            Error(ex, "SocketException occurred.");
        }

        public static void HandleGeneralException(Exception ex, string context)
        {
            Error(ex, $"Unexpected error in {context}");
        }

        public static void HandleGeneralException(Exception ex)
        {
            Error(ex, $"Unexpected error: {ex.Message}");
        }

        public static void Error(Exception ex)
        {  
            Console.ForegroundColor = ConsoleColor.Red;
            WriteMessage($"EXCEPTION ENCOUNTERED");
            WriteMessage($"Message: {ex.Message}");
            WriteMessage($"StackTrace: {ex.StackTrace}");
            WriteMessage($"Source: {ex.Source}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void Error(Exception ex, string message = "")
        {
            Console.ForegroundColor = ConsoleColor.Red;

            string shutdownInfo = _isShuttingDown ? " (During Shutdown)" : "";
            WriteMessage($"EXCEPTION ENCOUNTERED{shutdownInfo}{(string.IsNullOrEmpty(message) ? "" : $": {message}")}");
            WriteMessage($"Message: {ex.Message}");
            WriteMessage($"StackTrace: {ex.StackTrace}");
            WriteMessage($"Source: {ex.Source}");

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void BeginShutdown()
        {
            _isShuttingDown = true;
            Scry("Application shutdown initiated.");
        }

        public static async Task CloseWriter()
        {
            try
            {
                if (_writer != null)
                {
                    await _writer.FlushAsync();
                    Scry("Scribe: Writer flushed successfully.");
                    _writer.Close();
                    Scry("Scribe: Writer closed successfully.");
                }

                _writer?.Dispose();
                Scry("Scribe: Writer disposed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }


        private static void WriteMessage(string message,
            [System.Runtime.CompilerServices.CallerMemberName] string callerName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int callerLineNumber = 0)
        {
            string className = System.IO.Path.GetFileNameWithoutExtension(callerFilePath);

            string newMessage = $"[ {Constellations.TIMESTAMP}: {message} - {className}.{callerName}, From Line: {callerLineNumber} ]";

            Console.WriteLine(newMessage);

            if(!_isShuttingDown)
                _writer?.WriteLine(newMessage);
        }


    }
}
