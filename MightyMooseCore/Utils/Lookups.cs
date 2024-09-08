using Eco.Core;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Systems;
using Eco.Core.Utils;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Civics.Misc;
using Eco.Gameplay.Civics.Titles;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Skills;
using Eco.Moose.Data.Constants;
using Eco.Plugins.Networking;
using Eco.Shared.Items;
using Eco.Shared.Time;
using Eco.Shared.Utils;
using Eco.Simulation.Time;
using Eco.Simulation.WorldLayers;
using Eco.Simulation.WorldLayers.Layers;
using User = Eco.Gameplay.Players.User;

namespace Eco.Moose.Utils.Lookups
{
    public static class Lookups
    {
        // --- Gameplay ---

        public static double SecondsPassedTotal => WorldTime.Seconds;
        public static double SecondsPassedOnDay => WorldTime.Seconds % Constants.SECONDS_PER_DAY;
        public static double SecondsLeftOnDay => Constants.SECONDS_PER_DAY - SecondsPassedOnDay;
        public static double SecondsLeftUntilMeteor => TimeUtil.DaysToSeconds(DifficultySettingsConfig.Advanced.MeteorImpactInDays) - SecondsPassedTotal;

        public static int NumTotalPlayers => Users.Count();
        public static int NumOnlinePlayers => OnlineUsers.Count();
        public static int NumExhaustedPlayers => Users.Count(user => user.ExhaustionMonitor?.IsExhausted ?? false);

        public static IEnumerable<User> Users => UserManager.Users;
        public static IEnumerable<User> UsersAlphabetical => UserManager.Users.OrderBy(user => user.Name);
        public static User UserByName(string userName) => UserManager.FindUserByName(userName);
        public static User UserById(int userId) => UserManager.FindUserByID(userId);
        public static User UserByNameOrId(string userNameOrId) => int.TryParse(userNameOrId, out int id) ? UserById(id) : UserByName(userNameOrId);
        public static User UserByStrangeOrSteamID(string strangeID, string steamID) => UserManager.FindUserByStrangeId(strangeID) ?? UserManager.FindUserBySteamId(steamID);
        public static IEnumerable<User> OnlineUsers => UserManager.OnlineUsers.NonNull().Where(user => user.Client != null && user.Client.Connected);
        public static IEnumerable<User> OnlineUsersAlphabetical => OnlineUsers.OrderBy(user => user.Name);
        public static User OnlineUserByName(string userName) => OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(userName));
        public static User OnlineUserById(int userId) => OnlineUsers.FirstOrDefault(user => user.Id == userId);
        public static User OnlineUserByNameId(string userNameOrId) => int.TryParse(userNameOrId, out int id) ? OnlineUserById(id) : OnlineUserByName(userNameOrId);
        public static User OnlineUserByStrangeOrStrangeID(string strangeID, string steamID) => OnlineUsers.FirstOrDefault(user => user.SteamId.Equals(steamID) || user.StrangeId.Equals(strangeID));

        public static IEnumerable<Settlement> Settlements => Registrars.Get<Settlement>().NonNull();
        public static IEnumerable<Settlement> ActiveSettlements => Settlements.Where(settlement => settlement.IsActive);
        public static IEnumerable<Settlement> SettlementsWithActiveUsers => ActiveSettlements.Where(settlement => settlement.Citizens.Any(user => user.IsActive));
        public static Settlement SettlementByName(string settlementName) => Settlements.FirstOrDefault(settlement => settlement.Name.EqualsCaseInsensitive(settlementName));
        public static Settlement SettlementById(int settlementId) => Settlements.FirstOrDefault(settlement => settlement.Id == settlementId);
        public static Settlement SettlementByNameOrId(string settlementNameOrId) => int.TryParse(settlementNameOrId, out int id) ? SettlementById(id) : SettlementByName(settlementNameOrId);

        public static IEnumerable<Election> ActiveElections => ActiveSettlements.SelectMany(ActiveElectionsForSettlement);
        public static IEnumerable<Election> ActiveElectionsForSettlement(Settlement settlement) => CivicsUtils.AllActive<Election>(settlement);
        public static Election ActiveElectionByName(string electionName) => ActiveElections.FirstOrDefault(election => election.Name.EqualsCaseInsensitive(electionName));
        public static Election ActiveElectionById(int electionId) => ActiveElections.FirstOrDefault(election => election.Id == electionId);
        public static Election ActiveElectionByNameOrId(string electionNameOrId) => int.TryParse(electionNameOrId, out int id) ? ActiveElectionById(id) : ActiveElectionByName(electionNameOrId);

        public static IEnumerable<Law> ActiveLaws => ActiveSettlements.SelectMany(ActiveLawsForSettlement);
        public static IEnumerable<Law> ActiveLawsForSettlement(Settlement settlement) => CivicsUtils.AllActive<Law>(settlement);
        public static Law ActiveLawByName(string lawName) => ActiveLaws.FirstOrDefault(law => law.Name.EqualsCaseInsensitive(lawName));
        public static Law ActiveLawById(int lawId) => ActiveLaws.FirstOrDefault(law => law.Id == lawId);
        public static Law ActiveLawByNameByNameOrId(string lawNameOrId) => int.TryParse(lawNameOrId, out int id) ? ActiveLawById(id) : ActiveLawByName(lawNameOrId);

        public static IEnumerable<Demographic> ActiveDemographics => ActiveSettlements.SelectMany(ActiveDemographicsForSettlement).Concat(DemographicManager.Obj.SpecialEntries);
        public static IEnumerable<Demographic> ActiveDemographicsForSettlement(Settlement settlement) => DemographicManager.Obj.ActiveAndValidDemographics(settlement);
        public static Demographic ActiveDemographicByName(string demographicName) => ActiveDemographics.FirstOrDefault(demographic => demographic.Name.EqualsCaseInsensitive(demographicName));
        public static Demographic ActiveDemographicById(int demographicId) => ActiveDemographics.FirstOrDefault(demographic => demographic.Id == demographicId);
        public static Demographic ActiveDemographicByNameOrId(string demographicNameOrId) => int.TryParse(demographicNameOrId, out int id) ? ActiveDemographicById(id) : ActiveDemographicByName(demographicNameOrId);

        public static IEnumerable<WorkParty> ActiveWorkParties => Registrars.Get<WorkParty>().NonNull().Where(wp => wp.State == ProposableState.Active);
        public static WorkParty ActiveWorkPartyByName(string workPartyName) => ActiveWorkParties.FirstOrDefault(wp => wp.Name.EqualsCaseInsensitive(workPartyName));
        public static WorkParty ActiveWorkPartyById(int workPartyId) => ActiveWorkParties.FirstOrDefault(wp => wp.Id == workPartyId);
        public static WorkParty ActiveWorkPartyByNameOrId(string workPartyNameOrId) => int.TryParse(workPartyNameOrId, out int id) ? ActiveWorkPartyById(id) : ActiveWorkPartyByName(workPartyNameOrId);

        public static IEnumerable<ElectedTitle> ActiveElectedTitles => ActiveSettlements.SelectMany(ActiveElectedTitlesForSettlement);
        public static IEnumerable<ElectedTitle> ActiveElectedTitlesForSettlement(Settlement settlement) => CivicsUtils.AllActive<ElectedTitle>(settlement);
        public static ElectedTitle ActiveElectedTitleByName(string electedTitleName) => ActiveElectedTitles.FirstOrDefault(t => t.Name.EqualsCaseInsensitive(electedTitleName));
        public static ElectedTitle ActiveElectedTitleById(int electedTitleId) => ActiveElectedTitles.FirstOrDefault(wp => wp.Id == electedTitleId);
        public static ElectedTitle ActiveElectedTitleByNameOrId(string electedTitleNameOrId) => int.TryParse(electedTitleNameOrId, out int id) ? ActiveElectedTitleById(id) : ActiveElectedTitleByName(electedTitleNameOrId);

        public static IEnumerable<AppointedTitle> ActiveAppointedTitles => Registrars.Get<Title>().NonNull().Where(t => t is AppointedTitle && ((AppointedTitle)t).DirectOccupants.Count() > 0).Cast<AppointedTitle>();
        public static AppointedTitle ActiveAppointedTitleByName(string appointedTitleName) => ActiveAppointedTitles.FirstOrDefault(t => t.Name.EqualsCaseInsensitive(appointedTitleName));
        public static AppointedTitle ActiveAppointedTitleById(int appointedTitleId) => ActiveAppointedTitles.FirstOrDefault(wp => wp.Id == appointedTitleId);
        public static AppointedTitle ActiveAppointedTitleByNameOrId(string appointedTitleNameOrId) => int.TryParse(appointedTitleNameOrId, out int id) ? ActiveAppointedTitleById(id) : ActiveAppointedTitleByName(appointedTitleNameOrId);

        public static IEnumerable<Currency> Currencies => CurrencyManager.Currencies;
        public static Currency CurrencyByName(string currencyName) => Currencies.FirstOrDefault(c => c.Name.EqualsCaseInsensitive(currencyName));
        public static Currency CurrencyById(int currencyId) => Currencies.FirstOrDefault(c => c.Id == currencyId);
        public static Currency CurrencyByNameOrId(string currencyNameOrId) => int.TryParse(currencyNameOrId, out int id) ? CurrencyById(id) : CurrencyByName(currencyNameOrId);

        public static IEnumerable<Deed> Deeds => PropertyManager.Obj.Deeds;
        public static Deed DeedByName(string deedName) => Deeds.FirstOrDefault(deed => deed.Name.EqualsCaseInsensitive(deedName));
        public static Deed DeedById(int deedId) => Deeds.FirstOrDefault(deed => deed.Id == deedId);
        public static Deed DeedByNameOrId(string deedNameOrId) => int.TryParse(deedNameOrId, out int id) ? DeedById(id) : DeedByName(deedNameOrId);

        public static IEnumerable<Skill> Specialties => SkillTree.AllSkillTrees.SelectMany(skilltree => skilltree.ProfessionChildren).Select(skilltree => skilltree.StaticSkill);
        public static Skill SpecialtyByName(string specialtyName) => Specialties.FirstOrDefault(specialty => specialty.Name.EqualsCaseInsensitive(specialtyName));

        public static IEnumerable<Skill> Professions => SkillTree.ProfessionSkillTrees.Select(skilltree => skilltree.StaticSkill);
        public static Skill ProfessionByName(string professionName) => Professions.FirstOrDefault(profession => profession.Name.EqualsCaseInsensitive(professionName));

        public static IEnumerable<FoodItem> FoodItems => Item.AllItemsIncludingHidden.OfType<FoodItem>();

        public static IEnumerable<WorldLayer> Layers => WorldLayerManager.Obj.Layers;
        public static IEnumerable<WorldLayer> VisibleLayers => WorldLayerManager.Obj.Layers.Where(layer => layer.IsVisible);
        public static WorldLayer LayerByName(string layerName) => Layers.FirstOrDefault(layer => layer.Name.StartWithCaseInsensitive(layerName));
        public static WorldLayer VisibleLayerByName(string layerName) => VisibleLayers.FirstOrDefault(layer => layer.Name.StartWithCaseInsensitive(layerName));

        // --- Non-Gameplay ---

        public static IEnumerable<IServerPlugin> Plugins => PluginManager.Controller.Plugins;
        public static IEnumerable<IConfigurablePlugin> ConfigurablePlugins => Plugins.Where(plugin => plugin.GetType().GetInterfaces().Contains(typeof(IConfigurablePlugin))).Select(plugin => plugin as IConfigurablePlugin);
        public static string WebServerUrl => !NetworkManager.Config.WebServerUrl.IsEmpty() ? NetworkManager.Config.WebServerUrl : !NetworkManager.Config.RemoteAddress.IsEmpty() ? $"{NetworkManager.Config.RemoteAddress}:{NetworkManager.Config.WebServerPort}" : string.Empty;
    }
}
