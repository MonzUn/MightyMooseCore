using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.EW.Tools;
using System.Reflection;

namespace Eco.EW.Plugins
{
    [Priority(PriorityAttribute.VeryHigh)] // Need to start before any dependent plugins
    public class EcoWorldCore : IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
    {
        public readonly Version PluginVersion = Assembly.GetExecutingAssembly().GetName().Version;

        public static EcoWorldCore Obj { get { return PluginManager.GetPlugin<EcoWorldCore>(); } }
        public ThreadSafeAction<object, string> ParamChanged { get; set; }
        public string Status
        {
            get { return status; }
            private set
            {
                Logger.Debug($"Plugin status changed from \"{status}\" to \"{value}\"");
                status = value;
            }
        }
        private string status = "Uninitialized";

        private readonly PluginConfig<EcoWorldCoreConfig> config = new PluginConfig<EcoWorldCoreConfig>("EcoWorldCore");

        private const string ModIODeveloperToken = ""; // This will always be empty for all but actual release builds.
        private const string ModIOAppID = "";

        public override string ToString() => "EcoWorld Core";
        public string GetCategory() => "EcoWorld Mods";
        public string GetStatus() => Status;
        public IPluginConfig PluginConfig => config;
        public EcoWorldCoreConfig ConfigData => config.Config;
        public object GetEditObject() => config.Config;
        public void OnEditObjectChanged(object o, string param) => ConfigData.OnConfigChanged(param);

        public void Initialize(TimedTask timer)
        {
            Logger.RegisterLogger("EcoWorldCore", ConsoleColor.Green, ConfigData.LogLevel);

            Status = "Initializing";

            if (!string.IsNullOrWhiteSpace(ModIOAppID) && !string.IsNullOrWhiteSpace(ModIODeveloperToken)) // Only check for mod versioning if the data required for it exists
                VersionChecker.CheckVersion("EcoWorldCore", ModIOAppID, ModIODeveloperToken);
            else
                Logger.Info($"Plugin version is {PluginVersion}");

            Status = "Running";
        }
    }
}
