using Eco.Core.Serialization;
using Eco.ModKit;
using Eco.Shared.Utils;
using System.Diagnostics;
using System.Reflection;
using static Eco.Moose.Utils.Console.Console;

namespace Eco.Moose.Tools.VersionChecker
{
    using Eco.Moose.Tools.Logger;

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

        public static async Task<Version?> CheckVersion(string modName, string modIoModId, string modIoApiKey, int versionOutputComponentCount = 3)
        {
            ModIoData? modIoData = await GetModIoData(modIoModId, modIoApiKey);
            InstalledModData? installedData = GetInstalledModData(modName);
            if (modIoData == null || installedData == null || modIoData.Version == null || installedData.Version == null)
            {
                Logger.Error($"Failed to retreive mod version", Assembly.GetCallingAssembly());
                return null;
            }

            ConsoleOutputComponent[] components;
            if (modIoData.Version.Major == installedData.Version.Major && modIoData.Version.Minor == installedData.Version.Minor && modIoData.Version.Build == installedData.Version.Build)
            {
                components = new[]
                {
                    new ConsoleOutputComponent($"{modName} ", ConsoleColor.Green),
                    new ConsoleOutputComponent($"- Installed version ", ConsoleColor.Yellow),
                    new ConsoleOutputComponent($"({installedData.Version.ToString(versionOutputComponentCount)}) - Up to date!", ConsoleColor.Green),
                };

            }
            else if (modIoData.Version > installedData.Version)
            {
                components = new[]
                {
                    new ConsoleOutputComponent($"{modName} ", ConsoleColor.Green),
                    new ConsoleOutputComponent($"- Installed version ", ConsoleColor.Yellow),
                    new ConsoleOutputComponent($"({installedData.Version.ToString(versionOutputComponentCount)}) - Outdated!\n", ConsoleColor.Red),
                    new ConsoleOutputComponent($"Please download version ", ConsoleColor.Yellow),
                    new ConsoleOutputComponent($"({modIoData.Version.ToString(versionOutputComponentCount)}) ", ConsoleColor.Cyan),
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
                    new ConsoleOutputComponent($"({installedData.Version.ToString(versionOutputComponentCount)}) - Unreleased", ConsoleColor.Cyan),
                };
            }
            PrintConsoleColored(components);
            return modIoData.Version;
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
                Logger.Exception($"Failed to fetch mod metadata from ModIO", e, Assembly.GetCallingAssembly());
                return null;
            }

            if (modIoJsonResult == null)
            {
                Logger.Error($"ModIO mod metadata was null", Assembly.GetCallingAssembly());
                return null;
            }

            ModIoRequestData? modIoRequestResult = SerializationUtils.DeserializeJson<ModIoRequestData>(modIoJsonResult);
            if (modIoRequestResult == null)
            {
                Logger.Error($"Failed to parse ModIO mod metadata", Assembly.GetCallingAssembly());
                return null;
            }

            if (modIoRequestResult.Status != 1)
            {
                Logger.Error($"Mod metadata returned invalid status", Assembly.GetCallingAssembly());
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
                Logger.Warning($"Failed to parse ModIO mod version string", Assembly.GetCallingAssembly());
            }

            return modIoData;
        }

        private static InstalledModData? GetInstalledModData(string productName)
        {
            Version? version = null;
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
                        Logger.Warning($"Failed to parse installed mod version string", Assembly.GetCallingAssembly());
                        return null;
                    }
                    break;
                }
            }
            return new InstalledModData { Version = version };
        }
    }
}
