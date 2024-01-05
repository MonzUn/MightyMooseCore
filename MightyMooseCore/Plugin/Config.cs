using Eco.Moose.Tools.Logger;
using System.ComponentModel;

namespace Eco.Moose.Plugins
{
    public class MightyMooseCoreConfig
    {
        public static class DefaultValues
        {
            public static Logger.LogLevel LogLevel = Logger.LogLevel.Information;
        }

        public void OnConfigChanged(string param)
        {
            Logger.Trace("OnConfigChanged was invoked");

            if (param == nameof(LogLevel))
            {
                Logger.SetConfiguredLogLevel(LogLevel);
            }
        }

        [Description("Determines what message types will be printed to the server log. All message types below the selected one will be printed as well.")]
        public Logger.LogLevel LogLevel { get; set; } = DefaultValues.LogLevel;
    }
}
