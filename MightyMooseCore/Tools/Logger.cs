using Eco.Core.Utils.Logging;
using Eco.Shared.Localization;
using System.Reflection;
using static Eco.EW.Utils.Console;
using static Eco.Shared.Utils.ILogWriter;

namespace Eco.EW.Tools
{
    public static class Logger
    {
        public enum LogLevel
        {
            DebugVerbose, // Ignored unless the configured log level is also DebugVerbose
            Debug,
            Warning,
            Information,
            Error,
            Silent, // Prints to the log file only
        }

        private static ConsoleColor[] LogLevelColors = new ConsoleColor[]
        {
            ConsoleColor.Gray,          // DebugVerbose
            ConsoleColor.Gray,          // Debug
            ConsoleColor.DarkYellow,    // Warning
            ConsoleColor.White,         // Information
            ConsoleColor.DarkRed,       // Error
        };

        public struct LogData
        {
            public string Tag;
            public ConsoleColor TagColor;
            public LogLevel ConfiguredLevel;
            public NLogWriter Log;
        }

        private static Dictionary<Assembly, LogData> Loggers = new();

        public static bool RegisterLogger(string tag, ConsoleColor tagColor, LogLevel configuredLevel)
        {
            Assembly caller = Assembly.GetCallingAssembly();
            if (Loggers.ContainsKey(caller))
            {
                return false;
            }

            LogData data = new LogData
            {
                Tag = tag,
                TagColor = tagColor,
                ConfiguredLevel = configuredLevel,
                Log = NLogManager.GetLogWriter(tag),
            };

            Loggers.Add(caller, data);
            return true;
        }

        public static bool SetConfiguredLogLevel(LogLevel level)
        {
            Assembly? assembly = Assembly.GetCallingAssembly();
            if (Loggers.TryGetValue(assembly, out LogData logData))
            {
                logData.ConfiguredLevel = level;
                Loggers[assembly] = logData;
                return true;
            }
            return false;
        }

        public static void DebugVerbose(string message) => Write(message, LogLevel.DebugVerbose, Assembly.GetCallingAssembly());
        public static void Debug(string message) => Write(message, LogLevel.Debug, Assembly.GetCallingAssembly());

        public static void Warning(string message) => Write(message, LogLevel.Warning, Assembly.GetCallingAssembly());
        public static void DebugWarning(string message) => Write(message, LogLevel.Warning, Assembly.GetCallingAssembly(), onlyPrintConsoleIfDebug: true);

        public static void Info(string message) => Write(message, LogLevel.Information, Assembly.GetCallingAssembly());
        public static void DebugInfo(string message) => Write(message, LogLevel.Information, Assembly.GetCallingAssembly(), onlyPrintConsoleIfDebug: true);

        public static void Error(string message) => Write(message, LogLevel.Error, Assembly.GetCallingAssembly());
        public static void DebugError(string message) => Write(message, LogLevel.Error, Assembly.GetCallingAssembly(), onlyPrintConsoleIfDebug: true);
        public static void Exception(string message, Exception exception) => Write(message, LogLevel.Error, Assembly.GetCallingAssembly(), exception);
        public static void DebugException(string message, Exception exception) => Write(message, LogLevel.Error, Assembly.GetCallingAssembly(), exception, onlyPrintConsoleIfDebug: true);

        public static void Silent(string message) => Write(message, LogLevel.Silent, Assembly.GetCallingAssembly());

        private static void Write(string message, LogLevel level, Assembly caller, Exception? exception = null, bool onlyPrintConsoleIfDebug = false)
        {
            if (Loggers.TryGetValue(caller, out LogData logData))
            {
                switch (level)
                {
                    case LogLevel.DebugVerbose:
                        if (logData.ConfiguredLevel <= LogLevel.DebugVerbose)
                        {
                            PrintToConsole(message, LogLevel.DebugVerbose, logData);
                            logData.Log.Debug(FormatLogMessage(message)); // Verbose debug log messages are only written to the log file if enabled via configuration
                        }
                        break;

                    case LogLevel.Debug:
                        if (logData.ConfiguredLevel <= LogLevel.Debug)
                            PrintToConsole(message, LogLevel.Debug, logData);
                        logData.Log.Debug(FormatLogMessage(message));
                        break;

                    case LogLevel.Warning:
                        if (logData.ConfiguredLevel <= LogLevel.Warning && (!onlyPrintConsoleIfDebug || logData.ConfiguredLevel <= LogLevel.Debug))
                            PrintToConsole(message, LogLevel.Warning, logData);
                        logData.Log.WriteWarning(FormatLogMessage(message));
                        break;

                    case LogLevel.Information:
                        if (logData.ConfiguredLevel <= LogLevel.Information && (!onlyPrintConsoleIfDebug || logData.ConfiguredLevel <= LogLevel.Debug))
                            PrintToConsole(message, LogLevel.Information, logData);
                        logData.Log.Write(FormatLogMessage(message));
                        break;

                    case LogLevel.Error:
                        if (logData.ConfiguredLevel <= LogLevel.Error && (!onlyPrintConsoleIfDebug || logData.ConfiguredLevel <= LogLevel.Debug))
                            PrintToConsole($"{message}\nException: {exception}", LogLevel.Error, logData);

                        ErrorInfo errorInfo = new ErrorInfo(FormatLogMessage(message), exception);
                        logData.Log.WriteError(ref errorInfo, stripTagsForConsole: true);
                        break;

                    case LogLevel.Silent:
                        logData.Log.Write(FormatLogMessage(message));
                        break;
                }
            }
        }

        private static string FormatLogMessage(string message) => ($"[{DateTime.Now:hh:mm:ss}] {message}");

        private static void PrintToConsole(string message, LogLevel level, LogData logData)
        {
            ConsoleOutputComponent[] components = new[]
            {
                    new ConsoleOutputComponent(Localizer.DoStr($"[{logData.Tag}] "), logData.TagColor),
                    new ConsoleOutputComponent(Localizer.DoStr($"[{message}] "), LogLevelColors[(int)level]),
                };
            PrintConsoleColored(components);
        }
    }
}
