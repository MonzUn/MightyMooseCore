using Eco.Gameplay.Auth;
using Eco.Gameplay.Components;
using Eco.Gameplay.Components.Store;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Moose.Extensions;
using Eco.Moose.Plugin;
using Eco.Shared.IoC;
using Eco.Shared.Items;
using Eco.Shared.Utils;
using System.Text;
using static Eco.Shared.Mathf;
using LookupEntry = Eco.Moose.Data.Either<Eco.Gameplay.Items.Item, Eco.Gameplay.Players.User, Eco.Gameplay.Items.Tag, Eco.Gameplay.Components.Store.StoreComponent>;
using StoreOfferGroup = System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.Store.StoreComponent, Eco.Gameplay.Components.TradeOffer>>;
using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.Store.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Moose.Features
{
    public class Trade
    {
        public enum TradeTargetType
        {
            Tag,
            Item,
            User,
            Store,
            Invalid,
        }

        private static List<LookupEntry> _itemLookup = null;

        public static List<LookupEntry> ItemLookup =>
            _itemLookup ??= Item.AllItemsExceptHidden.Select(item => new LookupEntry(item)).ToList();

        private static List<LookupEntry> _tagLookup = null;

        public static List<LookupEntry> TagLookup =>
            _tagLookup ??= FindTags().Select(tag => new LookupEntry(tag)).ToList();

        public static List<LookupEntry> UserLookup => UserManager.Users.Select(user => new LookupEntry(user)).ToList();

        public static List<LookupEntry> StoreLookup => AllStores.Select(store => new LookupEntry(store)).ToList();

        public static IEnumerable<StoreComponent> AllStores => WorldObjectUtil.AllObjsWithComponent<StoreComponent>().Where(store => store.Owners != null);

        public static string StoreCurrencyName(StoreComponent store)
        {
            return store.CurrencyName.StripTags();
        }

        public static string FindOffers(string searchName, out TradeTargetType targetType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers)
        {
            List<string> entries = new List<string>();

            IEnumerable<LookupEntry> lookup = ItemLookup.Concat(TagLookup).Concat(UserLookup).Concat(StoreLookup);
            LookupEntry match = BestMatchOrDefault(searchName, lookup, entry =>
            {
                if (entry.Is<Tag>())
                    return entry.Get<Tag>().DisplayName;
                else if (entry.Is<Item>())
                    return entry.Get<Item>().DisplayName;
                else if (entry.Is<User>())
                    return entry.Get<User>().Name;
                else if (entry.Is<StoreComponent>())
                    return entry.Get<StoreComponent>().Parent.Name;
                else
                    return string.Empty;
            });

            string matchedName = string.Empty;
            targetType = TradeTargetType.Invalid;
            groupedBuyOffers = null;
            groupedSellOffers = null;
            if (match.Is<Tag>())
            {
                Tag matchTag = match.Get<Tag>();
                matchedName = matchTag.Name;

                bool filter(StoreComponent store, TradeOffer offer) => offer.Stack.Item.Tags().Contains(matchTag);
                groupedSellOffers = SellOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);

                targetType = TradeTargetType.Tag;
            }
            else if (match.Is<Item>())
            {
                Item matchItem = match.Get<Item>();
                matchedName = matchItem.DisplayName;

                bool filter(StoreComponent store, TradeOffer offer) => offer.Stack.Item.TypeID == matchItem.TypeID;
                groupedSellOffers = SellOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);

                targetType = TradeTargetType.Item;
            }
            else if (match.Is<User>())
            {
                User matchUser = match.Get<User>();
                matchedName = matchUser.Name;

                bool filter(StoreComponent store, TradeOffer offer) => store.Parent.Owners == matchUser;
                groupedSellOffers = SellOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);

                targetType = TradeTargetType.User;
            }
            else if (match.Is<StoreComponent>())
            {
                StoreComponent matchStore = match.Get<StoreComponent>();
                matchedName = matchStore.Parent.Name;

                bool filter(StoreComponent store, TradeOffer offer) => store == matchStore;
                groupedSellOffers = SellOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(tuple => StoreCurrencyName(tuple.Item1)).OrderBy(group => group.Key);

                targetType = TradeTargetType.Store;
            }

            return matchedName;
        }

        public static T? BestMatchOrDefault<T>(string query, IEnumerable<T> lookup, Func<T, string> getKey)
        {
            var orderedAndKeyed = lookup.Select(t => Tuple.Create(getKey(t), t)).OrderBy(t => t.Item1);
            var matches = new List<Predicate<string>> {
                k => k == query,
                k => k.StartWithCaseInsensitive(query),
                k => k.ContainsCaseInsensitive(query)
            };

            foreach (var matcher in matches)
            {
                var match = orderedAndKeyed.FirstOrDefault(t => matcher(t.Item1));
                if (match != default(Tuple<string, T>))
                {
                    return match.Item2;
                }
            }

            return default;
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
            return AllStores
                .SelectMany(store =>
                    storeToOffers(store)
                        .Where(offer => offer.IsSet && includeFilter(store, offer))
                        .Select(offer => Tuple.Create(store, offer)))
                .OrderBy(t => t.Item2.Stack.Item.DisplayName)
                .Skip(start)
                .Take(count);
        }

        private static IEnumerable<Tag> FindTags()
        {
            List<Tag> uniqueTags = new List<Tag>();
            foreach (Item item in Item.AllItemsExceptHidden)
            {
                foreach (Tag tag in item.Tags())
                {
                    if (!uniqueTags.Contains(tag))
                        uniqueTags.Add(tag);
                }
            }
            return uniqueTags;
        }

        public static void FormatTrades(User user, TradeTargetType tradeType, StoreOfferList groupedBuyOffers, StoreOfferList groupedSellOffers, out string message)
        {
            // Format message
            StringBuilder builder = new StringBuilder();
            if (groupedSellOffers.Count() > 0 || groupedBuyOffers.Count() > 0)
            {
                switch (tradeType)
                {
                    case TradeTargetType.Tag:
                    case TradeTargetType.Item:
                        groupedBuyOffers = groupedBuyOffers.OrderByDescending(o => o.First().Item1.Currency != null ? MooseStorage.WorldData.CurrencyToTradeCountMap.GetValueOrDefault(o.First().Item1.Currency.Id, 0) : int.MinValue); // Currency == null => Barter store
                        groupedSellOffers = groupedSellOffers.OrderByDescending(o => o.First().Item1.Currency != null ? MooseStorage.WorldData.CurrencyToTradeCountMap.GetValueOrDefault(o.First().Item1.Currency.Id, 0) : int.MinValue);
                        break;

                    case TradeTargetType.User:
                    case TradeTargetType.Store:
                        break;
                }

                foreach (StoreOfferGroup group in groupedBuyOffers)
                {
                    builder.AppendLine(Text.Bold(Text.Color(Color.Green, $"<--- Buying for {group.First().Item1.CurrencyName.StripTags()} --->")));
                    builder.Append(TradeOffersToDescriptions(group, user, tradeType));
                    builder.AppendLine();
                }

                foreach (StoreOfferGroup group in groupedSellOffers)
                {
                    builder.AppendLine(Text.Bold(Text.Color(Color.Red, $"<--- Selling for {group.First().Item1.CurrencyName.StripTags()} --->")));
                    builder.Append(TradeOffersToDescriptions(group, user, tradeType));
                    builder.AppendLine();
                }
            }
            else
            {
                builder.AppendLine("--- No trade offers available ---");
            }
            message = builder.ToString();
        }

        private static string TradeOffersToDescriptions(StoreOfferGroup offers, User user, TradeTargetType tradeType)
        {
            Func<Tuple<StoreComponent, TradeOffer>, string> getLabel = tradeType switch
            {
                TradeTargetType.Tag => t => $"{t.Item2.Stack.Item.MarkedUpName} @ {t.Item1.Parent.MarkedUpName}",
                TradeTargetType.Item => t => $"@ {t.Item1.Parent.MarkedUpName}",
                TradeTargetType.User => t => t.Item2.Stack.Item.MarkedUpName,
                TradeTargetType.Store => t => t.Item2.Stack.Item.MarkedUpName,
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
                    if(price > 0.0f && !float.IsInfinity(availableCurrency))
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
