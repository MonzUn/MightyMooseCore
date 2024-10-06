using Eco.Gameplay.Components.Store;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.TextLinks;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Shared.Utils;
using System.Reflection;
using static Eco.Moose.Data.Enums;

namespace Eco.Moose.Features
{
    public class DynamicLookup
    {
        private static Dictionary<LookupTypes, Func<IEnumerable<object>>> _lookupConnections = new Dictionary<LookupTypes, Func<IEnumerable<object>>>
        {
            // The ordering here determines search order and priority for perfect matches
            { LookupTypes.Item, Lookups.Items.Cast<object>},
            { LookupTypes.Tag, Lookups.Tags.Cast<object>},
            { LookupTypes.User, Lookups.Users.Cast<object>},
            { LookupTypes.Store, Lookups.Stores.Cast<object>},
        };

        public static LookupResult Lookup(string lookupName, LookupTypes typeFlag)
        {
            IReadOnlyList<object> lookupCollection = BuildLookupCollection(typeFlag);
            IReadOnlyList<object> matchingEntities = BuildMatchCollection(lookupName, lookupCollection);

            LookupResultTypes result = LookupResultTypes.NoMatch;
            string errorMessage = string.Empty;
            if (matchingEntities.Count == 0)
            {
                result = LookupResultTypes.NoMatch;
                errorMessage = $"Failed to find any entity matching \"{lookupName}\"";
            }
            else if (matchingEntities.Count == 1)
            {
                result = LookupResultTypes.SingleMatch;
            }
            else
            {
                result = LookupResultTypes.MultiMatch;
                string entitiesMatchDesc = string.Join(", ", matchingEntities.Select(entity => (entity as ILinkable)?.UILink() ?? GetEntityName(entity)));
                errorMessage = $"Please specify search term - Found multiple matching entries\n{entitiesMatchDesc}";
            }

            return new LookupResult(result, matchingEntities, errorMessage);
        }

        private static IReadOnlyList<object> BuildLookupCollection(LookupTypes typeFlag)
        {
            List<object> lookupCollection = new List<object>();
            foreach (LookupTypes type in Enum.GetValues(typeof(LookupTypes)))
            {
                if ((typeFlag & type) != 0)
                {
                    lookupCollection = lookupCollection.Union(_lookupConnections[type]()).ToList();
                }
            }
            return lookupCollection;
        }

        private static IReadOnlyList<object> BuildMatchCollection(string lookupName, IReadOnlyList<object> lookupCollection)
        {
            List<object> matchingEntities = new List<object>();
            foreach (object entity in lookupCollection)
            {
                string entityName = GetEntityName(entity);
                if (entityName.EqualsCaseInsensitive(lookupName))
                    return new List<object>() { entity }; // Eearly out if a perfect match is found

                if (StringExtensions.ContainsCaseInsensitive(entityName, lookupName))
                    matchingEntities.Add(entity);
            }

            return matchingEntities;
        }

        public static string GetEntityName(object entity)
        {
            if (entity is Item item)
                return item.DisplayName;
            else if (entity is Tag tag)
                return tag.DisplayName;
            else if (entity is User user)
                return user.Name;
            else if (entity is StoreComponent store)
                return store.Parent.Name;

            Logger.Warning("Failed to lookup name for unknown entity type.", Assembly.GetCallingAssembly());
            return string.Empty;
        }

        public static LookupTypes GetEntityType(object entity)
        {
            if (entity is Item item)
                return LookupTypes.Item;
            else if (entity is Tag tag)
                return LookupTypes.Tag;
            else if (entity is User user)
                return LookupTypes.User;
            else if (entity is StoreComponent store)
                return LookupTypes.Store;

            Logger.Warning("Failed resolve lookup type of unknown entity type.", Assembly.GetCallingAssembly());
            return LookupTypes.None;
        }
    }
}
