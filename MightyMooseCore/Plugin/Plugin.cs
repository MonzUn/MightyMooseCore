using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Tools.VersionChecker;
using System.Reflection;

namespace Eco.Moose.Plugins
{
    [Priority(PriorityAttribute.VeryHigh)] // Need to start before any dependent plugins
    public class MightyMooseCore : IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
    {
        public readonly Version InstalledVersion = Assembly.GetExecutingAssembly().GetName().Version;
        public Version? ModIOVersion { get; private set; } = null;
        public readonly string PluginName = "MightyMooseCore";

        public static MightyMooseCore Obj { get { return PluginManager.GetPlugin<MightyMooseCore>(); } }
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

        private readonly PluginConfig<MightyMooseCoreConfig> config = new PluginConfig<MightyMooseCoreConfig>("MightyMooseCore");

        private const string ModIODeveloperToken = ""; // This will always be empty for all but actual release builds.
        private const string ModIOAppID = "3561559";

        public override string ToString() => "Mighty Moose Core";
        public string GetCategory() => "Mighty Moose";
        public string GetStatus() => Status;
        public IPluginConfig PluginConfig => config;
        public MightyMooseCoreConfig ConfigData => config.Config;
        public object GetEditObject() => config.Config;
        public void OnEditObjectChanged(object o, string param) => ConfigData.OnConfigChanged(param);

        public async void Initialize(TimedTask timer)
        {
            Logger.RegisterLogger(PluginName, ConsoleColor.Green, ConfigData.LogLevel);

            Status = "Initializing";

            // Check mod versioning if the required data exists
            if (!string.IsNullOrWhiteSpace(ModIOAppID) && !string.IsNullOrWhiteSpace(ModIODeveloperToken))
                ModIOVersion = await VersionChecker.CheckVersion( PluginName, ModIOAppID, ModIODeveloperToken);
            else
                Logger.Info($"Plugin version is {InstalledVersion.ToString(3)}");

            Status = "Running";
        }
    }
}
