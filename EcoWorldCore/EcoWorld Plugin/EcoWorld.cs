using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.EW.Tools;

namespace Eco.EW.Plugins
{
    [Priority(PriorityAttribute.VeryHigh)] // Need to start before any dependent plugins
    public class EcoWorldCore : IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
    {
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
            Status = "Running";
        }
    }
}
