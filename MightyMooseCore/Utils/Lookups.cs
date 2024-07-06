using Eco.Core.Systems;
using Eco.Core.Utils;
using Eco.Gameplay.Civics;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Civics.Titles;
using Eco.Gameplay.Disasters;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Skills;
using Eco.Shared.Items;
using Eco.Shared.Utils;
using Eco.Simulation.Time;
using User = Eco.Gameplay.Players.User;

namespace Eco.Moose.Utils.Lookups
{
    using Constants = Constants.Constants;

    public static class Lookups
    {
        public static double SecondsPassedTotal => WorldTime.Seconds;
        public static double SecondsPassedOnDay => WorldTime.Seconds % Constants.SECONDS_PER_DAY;
        public static double SecondsLeftOnDay => Constants.SECONDS_PER_DAY - SecondsPassedOnDay;
        public static double SecondsLeftUntilMeteor => TimeUtil.DaysToSeconds(DisasterPlugin.MeteorImpactTime) - SecondsPassedTotal;

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

        public static IEnumerable<Election> ActiveElections => ElectionManager.Obj.CurrentElections(null).Where(election => election.Valid() && election.State == Shared.Items.ProposableState.Active);
        public static Election ActiveElectionByName(string electionName) => ActiveElections.FirstOrDefault(election => election.Name.EqualsCaseInsensitive(electionName));
        public static Election ActiveElectionByID(int electionID) => ActiveElections.FirstOrDefault(election => election.Id == electionID);
        public static Election ActiveElectionByNameOrID(string electionNameOrID) => int.TryParse(electionNameOrID, out int ID) ? ActiveElectionByID(ID) : ActiveElectionByName(electionNameOrID);

        public static IEnumerable<Law> ActiveLaws => CivicsData.Obj.Laws.NonNull().Where(law => law.State == ProposableState.Active);
        public static Law ActiveLawByName(string lawName) => ActiveLaws.FirstOrDefault(law => law.Name.EqualsCaseInsensitive(lawName));
        public static Law ActiveLawByID(int lawID) => ActiveLaws.FirstOrDefault(law => law.Id == lawID);
        public static Law ActiveLawByNameByNameOrID(string lawNameOrID) => int.TryParse(lawNameOrID, out int ID) ? ActiveLawByID(ID) : ActiveLawByName(lawNameOrID);

        public static IEnumerable<WorkParty> ActiveWorkParties => Registrars.Get<WorkParty>().NonNull().Where(wp => wp.State == ProposableState.Active);
        public static WorkParty ActiveWorkPartyByName(string workPartyName) => ActiveWorkParties.FirstOrDefault(wp => wp.Name.EqualsCaseInsensitive(workPartyName));
        public static WorkParty ActiveWorkPartyByID(int workPartyID) => ActiveWorkParties.FirstOrDefault(wp => wp.Id == workPartyID);
        public static WorkParty ActiveWorkPartyByNameOrID(string workPartyNameOrID) => int.TryParse(workPartyNameOrID, out int ID) ? ActiveWorkPartyByID(ID) : ActiveWorkPartyByName(workPartyNameOrID);

        public static IEnumerable<Demographic> ActiveDemographics => DemographicManager.Obj.ActiveAndValidDemographics(null);
        public static Demographic ActiveDemographicByName(string demographicName) => ActiveDemographics.FirstOrDefault(demographic => demographic.Name.EqualsCaseInsensitive(demographicName));
        public static Demographic ActiveDemographicByID(int demographicID) => ActiveDemographics.FirstOrDefault(demographic => demographic.Id == demographicID);
        public static Demographic ActiveDemographicByNameOrID(string demographicNameOrID) => int.TryParse(demographicNameOrID, out int ID) ? ActiveDemographicByID(ID) : ActiveDemographicByName(demographicNameOrID);

        public static IEnumerable<Title> ActiveTitles => Registrars.All<Title>().Where(x => x.CachedValidity == Result.Succeeded);
        public static Title ActiveTitleByName(string titleName) => ActiveTitles.FirstOrDefault(title => title.Name.EqualsCaseInsensitive(titleName));
        public static Title ActiveTitleByID(int titleID) => ActiveTitles.FirstOrDefault(title => title.Id == titleID);
        public static Title ActiveTitleByNameOrID(string titleNameOrID) => int.TryParse(titleNameOrID, out int ID) ? ActiveTitleByID(ID) : ActiveTitleByName(titleNameOrID);

        public static IEnumerable<Settlement> Settlements => Registrars.Get<Settlement>().NonNull().Where(settlement => settlement.Founded);
        public static IEnumerable<Settlement> ActiveSettlements => Settlements.Where(settlement => settlement.Citizens.Any(user => user.IsActive));
        public static Settlement SettlementByName(string settlementName) => Settlements.FirstOrDefault(settlement => settlement.Name.EqualsCaseInsensitive(settlementName));
        public static Settlement SettlementByID(int settlementID) => Settlements.FirstOrDefault(settlement => settlement.Id == settlementID);
        public static Settlement SettlementByNameOrID(string settlementNameOrID) => int.TryParse(settlementNameOrID, out int ID) ? SettlementByID(ID) : SettlementByName(settlementNameOrID);

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
