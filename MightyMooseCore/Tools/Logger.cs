using Eco.Core.Utils.Logging;
using Eco.Moose.Plugin;
using Eco.Shared.Localization;
using System.Reflection;
using static Eco.Moose.Utils.Console;
using static Eco.Shared.Utils.ILogWriter;

namespace Eco.Moose.Tools.Logger
{
    public static class Logger
    {
        private static bool serverInited = false;
        private static Mutex registerMutex = new Mutex();

        public enum LogLevel
        {
            Trace, // Trace log messages are only written to the log file if enabled via configuration
            Debug,
            Warning,
            Information,
            Error,
            Silent, // Prints to the log file only
        }

        private static ConsoleColor[] LogLevelColors = new ConsoleColor[]
        {
            ConsoleColor.Gray,          // Trace
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

        public static void PostServerInit()
        {
            serverInited = true;
        }

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

            registerMutex.WaitOne();
            Loggers.Add(caller, data);
            registerMutex.ReleaseMutex();

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

        public static void Trace(string message, Assembly? caller = null) => Write(message, LogLevel.Trace, caller ?? Assembly.GetCallingAssembly());
        public static void Debug(string message, Assembly? caller = null) => Write(message, LogLevel.Debug, caller ?? Assembly.GetCallingAssembly());

        public static void Warning(string message, Assembly? caller = null) => Write(message, LogLevel.Warning, caller ?? Assembly.GetCallingAssembly());
        public static void DebugWarning(string message, Assembly? caller = null) => Write(message, LogLevel.Warning, caller ?? Assembly.GetCallingAssembly(), onlyPrintConsoleIfDebug: true);

        public static void Info(string message, Assembly? caller = null) => Write(message, LogLevel.Information, caller ?? Assembly.GetCallingAssembly());
        public static void DebugInfo(string message, Assembly? caller = null) => Write(message, LogLevel.Information, caller ?? Assembly.GetCallingAssembly(), onlyPrintConsoleIfDebug: true);

        public static void Error(string message, Assembly? caller = null) => Write(message, LogLevel.Error, caller ?? Assembly.GetCallingAssembly());
        public static void DebugError(string message, Assembly? caller = null) => Write(message, LogLevel.Error, caller ?? Assembly.GetCallingAssembly(), onlyPrintConsoleIfDebug: true);
        public static void Exception(string message, Exception exception, Assembly? caller = null) => Write(message, LogLevel.Error, caller ?? Assembly.GetCallingAssembly(), exception);
        public static void DebugException(string message, Exception exception, Assembly? caller = null) => Write(message, LogLevel.Error, caller ?? Assembly.GetCallingAssembly(), exception, onlyPrintConsoleIfDebug: true);

        public static void Silent(string message, Assembly? caller = null) => Write(message, LogLevel.Silent, caller ?? Assembly.GetCallingAssembly());

        private static void Write(string message, LogLevel level, Assembly caller, Exception? exception = null, bool onlyPrintConsoleIfDebug = false)
        {
            // Protect the logger register from being used while a registration is in progress during server init
            bool lockedMutex = false;
            if (!serverInited)
                lockedMutex = registerMutex.WaitOne();

            bool foundLogData = Loggers.TryGetValue(caller, out LogData logData);

            if (lockedMutex)
                registerMutex.ReleaseMutex();

            if (foundLogData)
            {
                switch (level)
                {
                    case LogLevel.Trace:
                        if (logData.ConfiguredLevel <= LogLevel.Trace)
                        {
                            PrintToConsole(message, LogLevel.Trace, logData);
                            logData.Log.Write(FormatLogMessage(message, LogLevel.Trace)); // Trace log messages are only written to the log file if enabled via configuration
                        }
                        break;

                    case LogLevel.Debug:
                        if (logData.ConfiguredLevel <= LogLevel.Debug)
                            PrintToConsole(message, LogLevel.Debug, logData);
                        logData.Log.Write(FormatLogMessage(message, LogLevel.Debug));
                        break;

                    case LogLevel.Warning:
                        if (logData.ConfiguredLevel <= LogLevel.Warning && (!onlyPrintConsoleIfDebug || logData.ConfiguredLevel <= LogLevel.Debug))
                            PrintToConsole(message, LogLevel.Warning, logData);
                        logData.Log.WriteWarning(FormatLogMessage(message, LogLevel.Warning));
                        break;

                    case LogLevel.Information:
                        if (logData.ConfiguredLevel <= LogLevel.Information && (!onlyPrintConsoleIfDebug || logData.ConfiguredLevel <= LogLevel.Debug))
                            PrintToConsole(message, LogLevel.Information, logData);
                        logData.Log.Write(FormatLogMessage(message, LogLevel.Information));
                        break;

                    case LogLevel.Error:
                        if (logData.ConfiguredLevel <= LogLevel.Error && (!onlyPrintConsoleIfDebug || logData.ConfiguredLevel <= LogLevel.Debug))
                        {
                            if (exception != null)
                            {
                                message += $"\nException: {exception}";
                            }
                            PrintToConsole(message, LogLevel.Error, logData);
                        }

                        ErrorInfo errorInfo = new ErrorInfo(FormatLogMessage(message, LogLevel.Error), exception);
                        logData.Log.WriteError(ref errorInfo, stripTagsForConsole: true);
                        break;

                    case LogLevel.Silent:
                        logData.Log.Write(FormatLogMessage(message, LogLevel.Information));
                        break;
                }
            }
            else
            {
                ConsoleOutputComponent[] components = new[]
                {
                    new ConsoleOutputComponent(Localizer.DoStr($"[{MightyMooseCore.Obj.PluginName}] "), ConsoleColor.Green),
                    new ConsoleOutputComponent(Localizer.DoStr($"[{LogLevel.Error}] "), LogLevelColors[(int)LogLevel.Error]),
                    new ConsoleOutputComponent(Localizer.DoStr($"Assembly \"{caller.GetName()}\" attempted to log without first registering a logger.\nMessage: {message}"), LogLevelColors[(int)LogLevel.Error]),
                };
                PrintConsoleColored(components);
            }
        }

        private static string FormatLogMessage(string message, LogLevel level) => ($"[{level}] {message}");

        private static void PrintToConsole(string message, LogLevel level, LogData logData)
        {
            ConsoleOutputComponent[] components = new[]
            {
                    new ConsoleOutputComponent(Localizer.DoStr($"[{logData.Tag}] "), logData.TagColor),
                    new ConsoleOutputComponent(Localizer.DoStr($"[{level}] "), LogLevelColors[(int)level]),
                    new ConsoleOutputComponent(Localizer.DoStr(message), LogLevelColors[(int)level]),
            };
            PrintConsoleColored(components);
        }
    }
}
