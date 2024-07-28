using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Systems;
using Eco.Core.Utils;
using Eco.Gameplay.Aliases;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Property;
using Eco.Gameplay.Settlements;
using Eco.Moose.Events;
using Eco.Moose.Events.Converter;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Tools.VersionChecker;
using Eco.Moose.Utils.Lookups;
using Eco.Shared.Utils;
using Eco.WorldGenerator;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Eco.Moose.Plugin
{
    [Priority(PriorityAttribute.VeryHigh)] // Need to start before any dependent plugins
    public class MightyMooseCore : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IConfigurablePlugin, IGameActionAware
    {
        public readonly string PluginName = "MightyMooseCore";
        public readonly Version InstalledVersion = Assembly.GetExecutingAssembly().GetName().Version;
        public Version? ModIoVersion { get; private set; } = null;

        public static readonly IConfiguration Secrets = new ConfigurationBuilder().AddUserSecrets<MightyMooseCore>().Build();
        public static readonly ThreadSafeAction<MooseEventArgs> OnEventFired = new ThreadSafeAction<MooseEventArgs>();

        private static string ModIoDeveloperToken = Secrets["ModIoDeveloperToken"];
        private const string ModIoAppId = "3561559";

        private bool _triggerWorldResetEvent = false;
        private Action<MooseEventArgs> OnEventConverted;

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

        public override string ToString() => PluginName.AddSpacesBetweenCapitals();
        public string GetCategory() => "Mighty Moose";
        public string GetStatus() => Status;
        public IPluginConfig PluginConfig => config;
        public MightyMooseCoreConfig ConfigData => config.Config;
        public object GetEditObject() => config.Config;
        public void OnEditObjectChanged(object o, string param) => ConfigData.OnConfigChanged(param);
        public LazyResult ShouldOverrideAuth(IAlias alias, IOwned property, GameAction action) => LazyResult.FailedNoMessage;

        public async void Initialize(TimedTask timer)
        {
            InitCallbacks();

            Logger.RegisterLogger(PluginName, ConsoleColor.Green, ConfigData.LogLevel);

            Status = "Initializing";

            MooseStorage.Instance.Initialize();

            WorldGeneratorPlugin.OnFinishGenerate.AddUnique(this.HandleWorldReset);

            EventConverter.Instance.Initialize();

            // Check mod versioning if the required data exists
            if (!string.IsNullOrWhiteSpace(ModIoAppId) && !string.IsNullOrWhiteSpace(ModIoDeveloperToken))
                ModIoVersion = await VersionChecker.CheckVersion( PluginName, ModIoAppId, ModIoDeveloperToken);
            else
                Logger.Info($"Plugin version is {InstalledVersion.ToString(3)}");

            PluginManager.Controller.RunIfOrWhenInited(PostServerInitialize); // Defer some initialization for when the server initialization is completed

            Status = "Running";
        }

        private async void PostServerInitialize()
        {
            Logger.PostServerInit();

            if (_triggerWorldResetEvent)
            {
                await HandleEvent(EventType.WorldReset, null);
                _triggerWorldResetEvent = false;
            }

            RegisterCallbacks();
        }

        public async Task ShutdownAsync()
        {
            DeregisterCallbacks();

            MooseStorage.Instance.Shutdown();
        }

        private void InitCallbacks()
        {
            OnEventConverted = async args => await HandleEvent(args.EventType, args.Data);
        }

        private void RegisterCallbacks()
        {
            EventConverter.OnEventConverted.Add(OnEventConverted);

            foreach (Settlement settlement in Lookups.ActiveSettlements.Where(s => !s.Founded))
            {
                settlement.FoundedEvent.Add(async () => _ = HandleEvent(EventType.SettlementFounded, settlement));
            }
            Registrars.Get<Settlement>().Callbacks.OnAdd.Add((netObj, settlement) => ((Settlement)settlement).FoundedEvent.Add(async () => _ = HandleEvent(EventType.SettlementFounded, settlement)));

            ActionUtil.AddListener(this);
        }

        private void DeregisterCallbacks()
        {
            EventConverter.OnEventConverted.Remove(OnEventConverted);
            ActionUtil.RemoveListener(this);
        }

        public void ActionPerformed(GameAction action)
        {
            switch (action)
            {
                case CurrencyTrade currencyTrade:
                    _ = HandleEvent(EventType.Trade, currencyTrade);
                    break;

                default:
                    break;
            }
        }

        public async Task HandleEvent(EventType eventType, params object[] data)
        {
            Logger.Trace($"Event of type {eventType} received");

            EventConverter.Instance.HandleEvent(eventType, data);
            MooseStorage.Instance.HandleEvent(eventType, data);

            if((long)eventType > EventConstants.INTERNAL_EVENT_DIVIDER)
            {
                OnEventFired.Invoke(new MooseEventArgs(eventType, data));
            }
        }

        public void HandleWorldReset()
        {
            _triggerWorldResetEvent = true;
        }
    }
}
