using System;

namespace Grouper2.Utility
{
    public static class Log
    {
        private static readonly object _lock = new object();
# if DEBUG
        public static void Degub(string message, Exception e = null, object sender = null)
        {
            lock (_lock)
            {
                string output = $"[DEBUG] - {message}";
                if (e != null)
                {
                    output += $"\n\t[ERROR] - {e}";
                }
                if (sender != null)
                {
                    output += $"\n\t[EMITTER] - {sender.GetType().FullName}";
                }

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(output);
                Console.ResetColor();
            }
        }
#endif
        
        public static void Verbose(string message, Exception e = null, object sender = null)
        {
            lock (_lock)
            {
                string output = $"{message}";
                if (e != null)
                {
                    output += $"\n\t[ERROR] {e}";
                }
                if (sender != null)
                {
                    output += $"\n\t[EMITTER] {sender.GetType().FullName}";
                }
                Console.WriteLine(output);
            }
        }
        
        public static void Std(string message, Exception e = null, object sender = null)
        {
            lock (_lock)
            {
                string output = $"{message}";
                if (e != null)
                {
                    output += $"\n\t[ERROR] {e}";
                }
                if (sender != null)
                {
                    output += $"\n\t[EMITTER] {sender.GetType().FullName}";
                }
                Console.WriteLine(output);
            }
        }
    }
}