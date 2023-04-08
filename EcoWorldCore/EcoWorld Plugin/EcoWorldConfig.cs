using Eco.EW.Tools;
using System.ComponentModel;

namespace Eco.EW.Plugins
{
    public class EcoWorldCoreConfig
    {
        public static class DefaultValues
        {
            public static Logger.LogLevel LogLevel = Logger.LogLevel.Information;
        }

        public void OnConfigChanged(string param)
        {
            if (param == nameof(LogLevel))
            {
                Logger.SetConfiguredLogLevel(LogLevel);
            }
        }

        [Description("Determines what message types will be printed to the server log. All message types below the selected one will be printed as well.")]
        public Logger.LogLevel LogLevel { get; set; } = DefaultValues.LogLevel;
    }
}
