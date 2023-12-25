using Eco.Core.Serialization;
using Eco.ModKit;
using Eco.Shared.Utils;
using System.Diagnostics;
using static Eco.Moose.Utils.Console;

namespace Eco.Moose.Tools
{
    public static class VersionChecker
    {
        private static HttpClient _client = new();

        private class ModIoRequestData
        {
            public int Status { get; set; }
            public string? Profile_Url { get; set; }
            public Dictionary<string, object>? ModFile { get; set; }
        }

        private class InstalledModData
        {
            public Version? Version;
        }

        private class ModIoData
        {
            public Version? Version;
            public string? ModPageUrl;
        }

        public static async void CheckVersion(string modName, string modIoModId, string modIoApiKey)
        {
            ModIoData? modIoData = await GetModIoData(modIoModId, modIoApiKey);
            InstalledModData? installedData = GetInstalledModData(modName);
            if (modIoData == null || installedData == null || modIoData.Version == null || installedData.Version == null)
            {
                return;
            }

            ConsoleOutputComponent[] components;
            if (modIoData.Version.Major == installedData.Version.Major && modIoData.Version.Minor == installedData.Version.Minor && modIoData.Version.Build == installedData.Version.Build)
            {
                components = new[]
                {
                    new ConsoleOutputComponent($"{modName} ", ConsoleColor.Green),
                    new ConsoleOutputComponent($"- Installed version ", ConsoleColor.Yellow),
                    new ConsoleOutputComponent($"({installedData.Version}) - Up to date!", ConsoleColor.Green),
                };

            }
            else if (modIoData.Version > installedData.Version)
            {
                components = new[]
                {
                    new ConsoleOutputComponent($"{modName} ", ConsoleColor.Green),
                    new ConsoleOutputComponent($"- Installed version ", ConsoleColor.Yellow),
                    new ConsoleOutputComponent($"({installedData.Version}) - Outdated!\n", ConsoleColor.Red),
                    new ConsoleOutputComponent($"Please download version ", ConsoleColor.Yellow),
                    new ConsoleOutputComponent($"({modIoData.Version}) ", ConsoleColor.Cyan),
                    new ConsoleOutputComponent($"from ", ConsoleColor.Yellow),
                    new ConsoleOutputComponent($"{modIoData.ModPageUrl}", ConsoleColor.Gray),
                };
            }
            else
            {
                components = new[]
                {
                    new ConsoleOutputComponent($"{modName} ", ConsoleColor.Green),
                    new ConsoleOutputComponent($"- Installed version ", ConsoleColor.Yellow),
                    new ConsoleOutputComponent($"({installedData.Version}) - Unreleased", ConsoleColor.Cyan),
                };
            }
            PrintConsoleColored(components);
        }

        private static async Task<ModIoData?> GetModIoData(string modIoModId, string modIoApiKey)
        {
            string? modIoJsonResult = string.Empty;
            try
            {
                modIoJsonResult = await _client.GetStringAsync($"https://api.mod.io/v1/games/6/mods/{modIoModId}?api_key={modIoApiKey}");
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to fetch mod metadata for mod with ID {modIoModId} from ModIO", e);
                return null;
            }

            if (modIoJsonResult == null)
            {
                Logger.Error($"ModIO mod metadata for mod with ID {modIoModId} was null");
                return null;
            }

            ModIoRequestData? modIoRequestResult = SerializationUtils.DeserializeJson<ModIoRequestData>(modIoJsonResult);
            if (modIoRequestResult == null)
            {
                Logger.Error($"Failed to parse ModIO mod metadata for mod with ID {modIoModId}");
                return null;
            }

            if (modIoRequestResult.Status != 1)
            {
                Logger.Error($"Mod metadate for mod with ID {modIoModId} returned invalid status");
                return null;
            }

            ModIoData modIoData = new ModIoData();
            modIoData.ModPageUrl = modIoRequestResult.Profile_Url;

            string? versionStr = (string?)modIoRequestResult.ModFile["version"];
            try
            {
                modIoData.Version = Version.Parse(versionStr);
            }
            catch
            {
                Logger.Warning($"Failed to parse ModIO mod version string for mod with ID {modIoModId}");
            }

            return modIoData;
        }

        private static InstalledModData? GetInstalledModData(string productName)
        {
            Version version = null;
            string[] assemblies = Directory.GetFiles(ModKitPlugin.ModDirectory, "*.dll", SearchOption.AllDirectories);
            foreach (string assembly in assemblies)
            {
                FileVersionInfo? assemblyData = FileVersionInfo.GetVersionInfo(assembly);
                if (assemblyData != null && assemblyData.FileVersion != null && assemblyData.ProductName.EqualsCaseInsensitive(productName))
                {
                    try
                    {
                        version = Version.Parse(assemblyData.FileVersion);
                    }
                    catch
                    {
                        Logger.Warning($"Failed to parse installed mod version string for {productName}");
                        return null;
                    }
                    break;
                }
            }
            return new InstalledModData { Version = version };
        }
    }
}
