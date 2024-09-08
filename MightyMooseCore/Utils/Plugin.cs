using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Moose.Data.Constants;
using Eco.Shared.Utils;
using static Eco.Moose.Utils.TypeUtils.TypeUtils;

namespace Eco.Moose.Utils.Plugin
{
    public static class PluginUtils
    {
        public static async Task<Tuple<bool, string>> ReloadConfig(IConfigurablePlugin plugin)
        {
            IPluginConfig config = plugin.PluginConfig;

            if (IsAssignableToGenericType(config.GetConfig().GetType(), typeof(Singleton<>)))
            {
                return Tuple.Create(false, $"Cannot reload config as the \"{plugin}\" plugin uses a singleton config");
            }

            // TODO: Make the Name parameter accessible in vanilla and remove this reflection
            string configName = (config.GetType().GetProperty("Name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(config) as string ?? string.Empty);
            if(configName.IsEmpty())
            {
                return Tuple.Create(false, "Failed to resolve config file name");
            }

            string configFilename = $"{configName}.eco";
            string configTemplateFilename = $"{configFilename}.template";
            string filenameToLoad = (File.Exists(Constants.CONFIG_PATH_ABS + configFilename) ? configFilename : File.Exists(Constants.CONFIG_PATH_ABS + configTemplateFilename) ? configTemplateFilename : string.Empty);
            if (!filenameToLoad.IsEmpty())
            {
                await plugin.PluginConfig.LoadAsync(filenameToLoad);
                plugin.OnEditObjectChanged(null, "");
                return Tuple.Create(true, $"Reloaded {filenameToLoad}");
            }
            else
            {
                return Tuple.Create(false, "Failed to find config file");
            }
        }
    }
}
