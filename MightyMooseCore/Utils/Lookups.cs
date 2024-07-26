using Eco.Core.Systems;
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
using Eco.Shared.Items;
using Eco.Shared.Utils;
using System.Linq;
using User = Eco.Gameplay.Players.User;

namespace Eco.Moose.Utils.Lookups
{
    public static class Lookups
    {
        public static double SecondsPassedOnDay => Simulation.Time.WorldTime.Seconds % Constants.SECONDS_PER_DAY;
        public static double SecondsLeftOnDay => Constants.SECONDS_PER_DAY - SecondsPassedOnDay;

        public static int NumTotalPlayers => Users.Count();
        public static int NumOnlinePlayers => OnlineUsers.Count();
        public static int NumExhaustedPlayers => Users.Count(user => user.ExhaustionMonitor?.IsExhausted ?? false);

        public static IEnumerable<User> Users => UserManager.Users;
        public static IEnumerable<User> UsersAlphabetical => UserManager.Users.OrderBy(user => user.Name);
        public static User UserByName(string userName) => UserManager.FindUserByName(userName);
        public static User UserByID(int userID) => UserManager.FindUserByID(userID);
        public static User UserByNameOrID(string userNameOrID) => int.TryParse(userNameOrID, out int ID) ? UserByID(ID) : UserByName(userNameOrID);
        public static User UserBySteamOrSLGID(string steamID, string slgID) => UserManager.FindUserById(steamID, slgID);
        public static IEnumerable<User> OnlineUsers => UserManager.OnlineUsers.NonNull().Where(user => user.Client != null && user.Client.Connected);
        public static IEnumerable<User> OnlineUsersAlphabetical => OnlineUsers.OrderBy(user => user.Name);
        public static User OnlineUserByName(string userName) => OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(userName));
        public static User OnlineUserByID(int userID) => OnlineUsers.FirstOrDefault(user => user.Id == userID);
        public static User OnlineUserByNameID(string userNameOrID) => int.TryParse(userNameOrID, out int ID) ? OnlineUserByID(ID) : OnlineUserByName(userNameOrID);
        public static User OnlineUserBySteamOrSLGDID(string steamID, string slgID) => OnlineUsers.FirstOrDefault(user => user.SteamId.Equals(steamID) || user.SlgId.Equals(slgID));

        public static IEnumerable<Settlement> Settlements => Registrars.Get<Settlement>().NonNull();
        public static IEnumerable<Settlement> ActiveSettlements => Settlements.Where(settlement => settlement.IsActive);
        public static IEnumerable<Settlement> SettlementsWithActiveUsers => ActiveSettlements.Where(settlement => settlement.Citizens.Any(user => user.IsActive));
        public static Settlement SettlementByName(string settlementName) => Settlements.FirstOrDefault(settlement => settlement.Name.EqualsCaseInsensitive(settlementName));
        public static Settlement SettlementByID(int settlementID) => Settlements.FirstOrDefault(settlement => settlement.Id == settlementID);
        public static Settlement SettlementByNameOrID(string settlementNameOrID) => int.TryParse(settlementNameOrID, out int ID) ? SettlementByID(ID) : SettlementByName(settlementNameOrID);

        public static IEnumerable<Election> ActiveElections => ActiveSettlements.SelectMany(ActiveElectionsForSettlement);
        public static IEnumerable<Election> ActiveElectionsForSettlement(Settlement settlement) => CivicsUtils.AllActive<Election>(settlement);
        public static Election ActiveElectionByName(string electionName) => ActiveElections.FirstOrDefault(election => election.Name.EqualsCaseInsensitive(electionName));
        public static Election ActiveElectionByID(int electionID) => ActiveElections.FirstOrDefault(election => election.Id == electionID);
        public static Election ActiveElectionByNameOrID(string electionNameOrID) => int.TryParse(electionNameOrID, out int ID) ? ActiveElectionByID(ID) : ActiveElectionByName(electionNameOrID);

        public static IEnumerable<Law> ActiveLaws => ActiveSettlements.SelectMany(ActiveLawsForSettlement);
        public static IEnumerable<Law> ActiveLawsForSettlement(Settlement settlement) => CivicsUtils.AllActive<Law>(settlement);
        public static Law ActiveLawByName(string lawName) => ActiveLaws.FirstOrDefault(law => law.Name.EqualsCaseInsensitive(lawName));
        public static Law ActiveLawByID(int lawID) => ActiveLaws.FirstOrDefault(law => law.Id == lawID);
        public static Law ActiveLawByNameByNameOrID(string lawNameOrID) => int.TryParse(lawNameOrID, out int ID) ? ActiveLawByID(ID) : ActiveLawByName(lawNameOrID);

        public static IEnumerable<Demographic> ActiveDemographics => ActiveSettlements.SelectMany(ActiveDemographicsForSettlement).Concat(DemographicManager.Obj.SpecialEntries);
        public static IEnumerable<Demographic> ActiveDemographicsForSettlement(Settlement settlement) => DemographicManager.Obj.ActiveAndValidDemographics(settlement);
        public static Demographic ActiveDemographicByName(string demographicName) => ActiveDemographics.FirstOrDefault(demographic => demographic.Name.EqualsCaseInsensitive(demographicName));
        public static Demographic ActiveDemographicByID(int demographicID) => ActiveDemographics.FirstOrDefault(demographic => demographic.Id == demographicID);
        public static Demographic ActiveDemographicByNameOrID(string demographicNameOrID) => int.TryParse(demographicNameOrID, out int ID) ? ActiveDemographicByID(ID) : ActiveDemographicByName(demographicNameOrID);

        public static IEnumerable<WorkParty> ActiveWorkParties => Registrars.Get<WorkParty>().NonNull().Where(wp => wp.State == ProposableState.Active);
        public static WorkParty ActiveWorkPartyByName(string workPartyName) => ActiveWorkParties.FirstOrDefault(wp => wp.Name.EqualsCaseInsensitive(workPartyName));
        public static WorkParty ActiveWorkPartyByID(int workPartyID) => ActiveWorkParties.FirstOrDefault(wp => wp.Id == workPartyID);
        public static WorkParty ActiveWorkPartyByNameOrID(string workPartyNameOrID) => int.TryParse(workPartyNameOrID, out int ID) ? ActiveWorkPartyByID(ID) : ActiveWorkPartyByName(workPartyNameOrID);

        public static IEnumerable<ElectedTitle> ActiveElectedTitles => ActiveSettlements.SelectMany(ActiveElectedTitlesForSettlement);
        public static IEnumerable<ElectedTitle> ActiveElectedTitlesForSettlement(Settlement settlement) => CivicsUtils.AllActive<ElectedTitle>(settlement);
        public static ElectedTitle ActiveElectedTitleByName(string electedTitleName) => ActiveElectedTitles.FirstOrDefault(t => t.Name.EqualsCaseInsensitive(electedTitleName));
        public static ElectedTitle ActiveElectedTitleByID(int electedTitleID) => ActiveElectedTitles.FirstOrDefault(wp => wp.Id == electedTitleID);
        public static ElectedTitle ActiveElectedTitleByNameOrID(string electedTitleNameOrID) => int.TryParse(electedTitleNameOrID, out int ID) ? ActiveElectedTitleByID(ID) : ActiveElectedTitleByName(electedTitleNameOrID);

        public static IEnumerable<AppointedTitle> ActiveAppointedTitles => Registrars.Get<AppointedTitle>().NonNull().Where(t => t.UserSet.Count() > 0);
        public static AppointedTitle ActiveAppointedTitleByName(string appointedTitleName) => ActiveAppointedTitles.FirstOrDefault(t => t.Name.EqualsCaseInsensitive(appointedTitleName));
        public static AppointedTitle ActiveAppointedTitleByID(int appointedTitleID) => ActiveAppointedTitles.FirstOrDefault(wp => wp.Id == appointedTitleID);
        public static AppointedTitle ActiveAppointedTitleByNameOrID(string appointedTitleNameOrID) => int.TryParse(appointedTitleNameOrID, out int ID) ? ActiveAppointedTitleByID(ID) : ActiveAppointedTitleByName(appointedTitleNameOrID);

        public static IEnumerable<Currency> Currencies => CurrencyManager.Currencies;
        public static Currency CurrencyByName(string currencyName) => Currencies.FirstOrDefault(c => c.Name.EqualsCaseInsensitive(currencyName));
        public static Currency CurrencyByID(int currencyID) => Currencies.FirstOrDefault(c => c.Id == currencyID);
        public static Currency CurrencyByNameOrID(string currencyNameOrID) => int.TryParse(currencyNameOrID, out int ID) ? CurrencyByID(ID) : CurrencyByName(currencyNameOrID);

        public static IEnumerable<Deed> Deeds => PropertyManager.Obj.Deeds;
        public static Deed DeedByName(string deedName) => Deeds.FirstOrDefault(deed => deed.Name.EqualsCaseInsensitive(deedName));
        public static Deed DeedByID(int deedID) => Deeds.FirstOrDefault(deed => deed.Id == deedID);
        public static Deed DeedByNameOrID(string deedNameOrID) => int.TryParse(deedNameOrID, out int ID) ? DeedByID(ID) : DeedByName(deedNameOrID);

        public static IEnumerable<Skill> Specialties => SkillTree.AllSkillTrees.SelectMany(skilltree => skilltree.ProfessionChildren).Select(skilltree => skilltree.StaticSkill);
        public static Skill SpecialtyByName(string specialtyName) => Specialties.FirstOrDefault(specialty => specialty.Name.EqualsCaseInsensitive(specialtyName));

        public static IEnumerable<Skill> Professions => SkillTree.ProfessionSkillTrees.Select(skilltree => skilltree.StaticSkill);
        public static Skill ProfessionByName(string professionName) => Professions.FirstOrDefault(profession => profession.Name.EqualsCaseInsensitive(professionName));

        public static IEnumerable<FoodItem> FoodItems => Item.AllItemsIncludingHidden.OfType<FoodItem>();
    }
}
