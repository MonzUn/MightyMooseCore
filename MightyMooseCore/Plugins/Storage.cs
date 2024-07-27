using Eco.Gameplay.GameActions;
using Eco.Moose.Data.Constants;
using Eco.Moose.Events;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Persistance;

namespace Eco.Moose.Plugin
{
    public sealed class MooseStorage
    {
        private const string PERSISANT_STORAGE_FILE_NAME = "PersistentData.json";
        private const string WORLD_STORAGE_FILE_NAME = "WorldData.json";

        public static readonly MooseStorage Instance = new MooseStorage();

        public static PersistentStorageData PersistentData { get; private set; } = new PersistentStorageData();
        public static WorldStorageData WorldData { get; private set; } = new WorldStorageData();

        public void Initialize()
        {
            Read();
        }

        public void Shutdown()
        {
            Write();
        }

        public void ResetPersistentData()
        {
            PersistentData = new PersistentStorageData();
            Write(); // Make sure we don't read old data in case of an ungraceful shutdown
        }

        public void ResetWorldData()
        {
            WorldData = new WorldStorageData();
            Write(); // Make sure we don't read old data in case of an ungraceful shutdown
        }

        public void Write()
        {
            PersistentStorageData persistentData = PersistentData;
            if (Persistance.WriteJsonToFile<PersistentStorageData>(PersistentData, Constants.STORAGE_PATH_ABS, PERSISANT_STORAGE_FILE_NAME))
                PersistentData = persistentData;

            WorldStorageData worldData = WorldData;
            if (Persistance.WriteJsonToFile<WorldStorageData>(WorldData, Constants.STORAGE_PATH_ABS, WORLD_STORAGE_FILE_NAME))
                WorldData = worldData;
        }

        public void Read()
        {
            PersistentStorageData persistentData = PersistentData;
            if (Persistance.ReadJsonFromFile<PersistentStorageData>(Constants.STORAGE_PATH_ABS, PERSISANT_STORAGE_FILE_NAME, ref persistentData))
                PersistentData = persistentData;

            WorldStorageData worldData = WorldData;
            if (Persistance.ReadJsonFromFile<WorldStorageData>(Constants.STORAGE_PATH_ABS, WORLD_STORAGE_FILE_NAME, ref worldData))
                WorldData = worldData;
        }

        public void HandleEvent(EventType eventType, params object[] data)
        {
            switch (eventType)
            {
                case EventType.WorldReset:
                    Logger.Info("New world generated - Removing storage data for previous world");
                    ResetWorldData();
                    break;

                // Keep track of the amount of trades per currency
                case EventType.AccumulatedTrade:
                    if (!(data[0] is IEnumerable<List<CurrencyTrade>> accumulatedTrade))
                        return;

                    foreach (var list in accumulatedTrade)
                    {
                        if (list.Count <= 0)
                            continue;

                        // Make sure an entry exists for the currency
                        int currencyID = accumulatedTrade.First()[0].Currency.Id;
                        if (!WorldData.CurrencyToTradeCountMap.ContainsKey(currencyID))
                            WorldData.CurrencyToTradeCountMap.Add(currencyID, 0);

                        WorldData.CurrencyToTradeCountMap.TryGetValue(currencyID, out int tradeCount);
                        WorldData.CurrencyToTradeCountMap[currencyID] = tradeCount + 1;
                    }
                    break;

                default:
                    break;
            }
        }

        public class PersistentStorageData
        {
        }

        public class WorldStorageData
        {
            public Dictionary<int, int> CurrencyToTradeCountMap = new Dictionary<int, int>();
        }
    }
}
