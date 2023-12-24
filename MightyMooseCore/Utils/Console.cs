using Eco.Core.Utils.IO;
using Eco.Core.Utils.Logging;
using Eco.Shared.Localization;
using Eco.Shared.Utils;

namespace Eco.EW.Utils
{
    public static class Console
    {
        public struct ConsoleOutputComponent
        {
            public ConsoleOutputComponent(string message, ConsoleColor color) { this.Message = message; this.Color = color; }

            public string Message;
            public ConsoleColor Color;
        }

        public static void PrintConsoleColored(ConsoleOutputComponent[] outputComponents)
        {
            string? context = outputComponents.SelectNonNull(c => c.Message).ToString();
            if (context == null)
                return;

            string timestamp = Localizer.DoStr($"[{DateTime.Now:hh:mm:ss}] ");
            context = timestamp + context;

            lock (ConsoleLogWriter.Instance)
            {
                ConsoleSynchronizationContext.Instance.Post(context =>
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkGreen;
                    System.Console.Write(timestamp);

                    foreach (var component in outputComponents)
                    {
                        System.Console.ForegroundColor = component.Color;
                        System.Console.Write(Localizer.DoStr(component.Message));
                    }

                    System.Console.Write("\n");
                    System.Console.ResetColor();
                }, context);
            }
        }
    }
}
