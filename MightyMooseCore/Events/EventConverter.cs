using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.SystemUtils;
using Nito.AsyncEx;

namespace Eco.Moose.Events.Converter
{
    public sealed class EventConverter
    {
        public static readonly ThreadSafeAction<MooseEventArgs> OnEventConverted = new ThreadSafeAction<MooseEventArgs>();
        public static readonly EventConverter Instance = new EventConverter();

        private const int TRADE_POSTING_INTERVAL_MS = 1000;
        private readonly Dictionary<Tuple<int, int>, List<CurrencyTrade>> _accumulatedTrades = new Dictionary<Tuple<int, int>, List<CurrencyTrade>>();
        private Timer _tradePostingTimer = null;
        private readonly AsyncLock _accumulatedTradesoverlapLock = new AsyncLock();
        private readonly AsyncLock _accumulatedTradesLock = new AsyncLock();

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static EventConverter()
        {
        }

        private EventConverter()
        {
        }

        public void Initialize()
        {
            // Initialize trade accumulation
            _tradePostingTimer = new Timer(InnerArgs =>
            {
                using (_accumulatedTradesoverlapLock.Lock()) // Make sure this code isn't entered multiple times simultaniously
                {
                    try
                    {
                        if (_accumulatedTrades.Count > 0)
                        {
                            // Fire the accumulated event
                            List<CurrencyTrade>[] trades = null;
                            using (_accumulatedTradesLock.Lock())
                            {
                                trades = new List<CurrencyTrade>[_accumulatedTrades.Values.Count];
                                _accumulatedTrades.Values.CopyTo(trades, 0);
                                _accumulatedTrades.Clear();
                            }
                            FireEvent(EventType.AccumulatedTrade, (object)trades);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Exception($"Failed to accumulate trade events", e);
                    }
                }

            }, null, 0, TRADE_POSTING_INTERVAL_MS);
        }

        public void Shutdown()
        {
            SystemUtils.StopAndDestroyTimer(ref _tradePostingTimer);
        }

        public void HandleEvent(EventType eventType, params object[] data)
        {
            switch (eventType)
            {
                case EventType.Trade:
                    if (!(data[0] is CurrencyTrade tradeEvent))
                        return;

                    // Store the event in a list in order to accumulate trade events that should be considered as one. We do this as each item in a trade will fire an individual event and we want to summarize them
                    Tuple<int, int> IdTuple = new Tuple<int, int>(tradeEvent.Citizen.Id, (tradeEvent.WorldObject as WorldObject).ID);
                    List<CurrencyTrade> trades;
                    using (_accumulatedTradesLock.Lock())
                    {
                        _accumulatedTrades.TryGetValue(IdTuple, out trades);
                    }
                    if (trades == null)
                    {
                        trades = new List<CurrencyTrade>();
                        using (_accumulatedTradesLock.Lock())
                        {
                            _accumulatedTrades.Add(IdTuple, trades);
                        }
                    }
                    using (_accumulatedTradesLock.Lock())
                    {
                        trades.Add(tradeEvent);
                    }
                    break;

                default:
                    break;
            }
        }

        private void FireEvent(EventType eventType, params object[] data)
        {
            OnEventConverted.Invoke(new MooseEventArgs(eventType, data));
        }
    }
}
