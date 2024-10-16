﻿using Eco.Gameplay.Auth;
using Eco.Gameplay.Components;
using Eco.Gameplay.Components.Store;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Moose.Extensions;
using Eco.Moose.Plugin;
using Eco.Moose.Utils.Lookups;
using Eco.Shared.IoC;
using Eco.Shared.Items;
using Eco.Shared.Utils;
using System.Text;
using static Eco.Moose.Data.Enums;
using static Eco.Shared.Mathf;

using StoreOfferGroup = System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.Store.StoreComponent, Eco.Gameplay.Components.TradeOffer>>;
using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.Store.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Moose.Features
{
    public class TradeOfferList
    {
        public TradeOfferList(StoreOfferList buyOffers, StoreOfferList sellOffers)
        {
            BuyOffers = buyOffers;
            SellOffers = sellOffers;
            OfferCount = (UInt64)(BuyOffers.Count() + sellOffers.Count());
        }

        public StoreOfferList BuyOffers { get; private set; }
        public StoreOfferList SellOffers { get; private set; }
        public UInt64 OfferCount { get; private set; }
    }

    public class Trade
    {
        public static string StoreCurrencyName(StoreComponent store)
        {
            return store.CurrencyName.StripTags();
        }

        public static TradeOfferList FindOffers(object entity, LookupTypes entityType)
        {
            StoreOfferList groupedBuyOffers = null;
            StoreOfferList groupedSellOffers = null;
            if (entityType == LookupTypes.Item)
            {
                bool filter(StoreComponent store, TradeOffer offer) => offer.Stack.Item.TypeID == ((Item)entity).TypeID;
                groupedSellOffers = SellOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
            }
            else if (entityType == LookupTypes.Tag)
            {
                bool filter(StoreComponent store, TradeOffer offer) => offer.Stack.Item.Tags().Contains((Tag)entity);
                groupedSellOffers = SellOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
            }
            else if (entityType == LookupTypes.User)
            {
                bool filter(StoreComponent store, TradeOffer offer) => store.Parent.Owners.ContainsUser((User)entity);
                groupedSellOffers = SellOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
            }
            else if (entityType == LookupTypes.Store)
            {
                bool filter(StoreComponent store, TradeOffer offer) => store == (StoreComponent)entity;
                groupedSellOffers = SellOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
            }
            else
            {
                return null;
            }

            return new TradeOfferList(groupedBuyOffers, groupedSellOffers);
        }

        public static IEnumerable<Tuple<StoreComponent, TradeOffer>>
            SellOffers(Func<StoreComponent, TradeOffer, bool> includeFilter,
                       int start = 0,
                       int count = int.MaxValue)
        {
            return AllStoresToOffers(store => store.StoreData.SellOffers, includeFilter, start, count);
        }

        public static IEnumerable<Tuple<StoreComponent, TradeOffer>>
            BuyOffers(Func<StoreComponent, TradeOffer, bool> includeFilter,
                int start = 0,
                int count = int.MaxValue)
        {
            return AllStoresToOffers(store => store.StoreData.BuyOffers, includeFilter, start, count);
        }

        public static IEnumerable<Tuple<StoreComponent, TradeOffer>> AllStoresToOffers(Func<StoreComponent, IEnumerable<TradeOffer>> storeToOffers,
                   Func<StoreComponent, TradeOffer, bool> includeFilter,
                   int start = 0,
                   int count = int.MaxValue)
        {
            return Lookups.Stores
                .SelectMany(store =>
                    storeToOffers(store)
                        .Where(offer => offer.IsSet && includeFilter(store, offer))
                        .Select(offer => Tuple.Create(store, offer)))
                .OrderBy(t => t.Item2.Stack.Item.DisplayName)
                .Skip(start)
                .Take(count);
        }

        public static void FormatTrades(User user, LookupTypes lookupType, StoreOfferList groupedBuyOffers, StoreOfferList groupedSellOffers, out string message)
        {
            // Format message
            StringBuilder builder = new StringBuilder();
            if (groupedSellOffers.Count() > 0 || groupedBuyOffers.Count() > 0)
            {
                switch (lookupType)
                {
                    case LookupTypes.Item:
                    case LookupTypes.Tag:
                        groupedBuyOffers = groupedBuyOffers.OrderByDescending(o => o.First().Item1.Currency != null ? MooseStorage.WorldData.CurrencyToTradeCountMap.GetValueOrDefault(o.First().Item1.Currency.Id, 0) : int.MinValue); // Currency == null => Barter store
                        groupedSellOffers = groupedSellOffers.OrderByDescending(o => o.First().Item1.Currency != null ? MooseStorage.WorldData.CurrencyToTradeCountMap.GetValueOrDefault(o.First().Item1.Currency.Id, 0) : int.MinValue);
                        break;

                    case LookupTypes.User:
                    case LookupTypes.Store:
                        break;
                }

                foreach (StoreOfferGroup group in groupedBuyOffers)
                {
                    builder.AppendLine(Text.Bold(Text.Color(Color.Green, $"<--- Buying for {group.First().Item1.CurrencyName.StripTags()} --->")));
                    builder.Append(TradeOffersToDescriptions(group, user, lookupType));
                    builder.AppendLine();
                }

                foreach (StoreOfferGroup group in groupedSellOffers)
                {
                    builder.AppendLine(Text.Bold(Text.Color(Color.Red, $"<--- Selling for {group.First().Item1.CurrencyName.StripTags()} --->")));
                    builder.Append(TradeOffersToDescriptions(group, user, lookupType));
                    builder.AppendLine();
                }
            }
            else
            {
                builder.AppendLine("--- No trade offers available ---");
            }
            message = builder.ToString();
        }

        private static string TradeOffersToDescriptions(StoreOfferGroup offers, User user, LookupTypes lookupType)
        {
            Func<Tuple<StoreComponent, TradeOffer>, string> getLabel = lookupType switch
            {
                LookupTypes.Item => t => $"@ {t.Item1.Parent.MarkedUpName}",
                LookupTypes.Tag => t => $"{t.Item2.Stack.Item.MarkedUpName} @ {t.Item1.Parent.MarkedUpName}",
                LookupTypes.User => t => t.Item2.Stack.Item.MarkedUpName,
                LookupTypes.Store => t => t.Item2.Stack.Item.MarkedUpName,
                _ => t => string.Empty,
            };

            IAuthManager AuthManager = ServiceHolder<IAuthManager>.Obj;

            StringBuilder normalOffers = new StringBuilder();
            StringBuilder noQuantityOffers = new StringBuilder();
            StringBuilder disabledOffers = new StringBuilder();
            foreach (var storeAndoffer in offers)
            {
                StoreComponent store = storeAndoffer.Item1;
                TradeOffer offer = storeAndoffer.Item2;

                float price = offer.Price;
                int quantity = offer.Stack.Quantity;
                Currency currency = store.Currency;
                int maxTradeCount = Int32.MaxValue;
                if (currency != null) // If currency is null, the store is set to barter
                {
                    // Calculate how many items can be traded using the available money
                    float availableCurrency = offer.Buying ? store.BankAccount.GetCurrencyHoldingVal(currency) : user.GetWealthInCurrency(currency);
                    if (price > 0.0f && !float.IsInfinity(availableCurrency))
                    {
                        maxTradeCount = FloorToInt(availableCurrency / price);
                    }

                    if (offer.Buying)
                    {
                        if (offer.ShouldLimit && offer.MaxNumWanted < maxTradeCount) // If there is a buy limit that is lower than what can be afforded, lower to that limit
                            maxTradeCount = offer.MaxNumWanted;
                    }
                    else if (quantity < maxTradeCount)
                    {
                        maxTradeCount = quantity; // If there less items for sale than we can pay for, lower to the amount available for sale
                    }
                }

                var quantityString = (offer.Stack.Quantity == maxTradeCount || maxTradeCount == Int32.MaxValue)
                    ? $"{quantity}"
                    : $"{quantity} ({maxTradeCount})";
                var line = $"{quantityString} - ${price} {getLabel(storeAndoffer)}";

                // Apply color and sort
                if (!store.OnOff.Enabled || !AuthManager.IsAuthorized(store.Parent, user, AccessType.ConsumerAccess, null))
                {
                    line = Text.Color(Color.Red, line);
                    disabledOffers.AppendLine(line);
                }
                else if (offer.Stack.Quantity == 0)
                {
                    line = Text.Color(Color.Yellow, line);
                    noQuantityOffers.AppendLine(line);
                }
                else
                {
                    normalOffers.AppendLine(line);
                }
            }

            return $"{normalOffers}{noQuantityOffers}{disabledOffers}";
        }
    }
}
