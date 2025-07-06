using Eco.Gameplay.Components;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Skills;

namespace Eco.Moose.Data
{
    public static class CommandData
    {
        public class SpecialtyAssignmentLookupResult
        {
            public SpecialtyAssignmentLookupResult(bool includeNonRefundable, bool includeScrollNoStar, bool includeInactive, Settlement? settlement, IEnumerable<Skill> specialties, Dictionary<Skill, List<User>> playersPerSpecialty)
            {
                IncludeNonRefundable = includeNonRefundable;
                IncludeScrollNoStar = includeScrollNoStar;
                IncludeInactive = includeInactive;
                SettlementFilter = settlement;
                Specialties = specialties.ToList();
                PlayersPerSpecialty = playersPerSpecialty;

                PlayerCountPerSpecialty = new Dictionary<Skill, int>();
                foreach (var entry in playersPerSpecialty)
                {
                    PlayerCountPerSpecialty.Add(entry.Key, entry.Value.Count);
                }
            }

            public bool IncludeInactive { get; private set; }
            public bool IncludeNonRefundable { get; private set; }
            public bool IncludeScrollNoStar { get; private set; }
            public Settlement? SettlementFilter { get; private set; }
            public List<Skill> Specialties { get; private set; }
            public Dictionary<Skill, List<User>> PlayersPerSpecialty { get; private set; }
            public Dictionary<Skill, int> PlayerCountPerSpecialty { get; private set; }
        }
    }

    public class RepairBountyLookupResult
    {
        public RepairBountyLookupResult(bool includesInactive, Settlement? settlementFilter, User? userFilter)
        {
            IncludesInactive = includesInactive;
            SettlementFilter = settlementFilter;
            UserFilter = userFilter;
        }

        public bool IncludesInactive { get; private set; }
        public Settlement? SettlementFilter { get; private set; }
        public User? UserFilter { get; private set; }
        public List<RepairBountyLookupData> Bounties { get; private set; } = new List<RepairBountyLookupData>();
    }

    public class RepairBountyLookupData
    {
        public RepairBountyLookupData(WorldObject repairObject, float durabilityPercent, Currency paymentCurrency, float paymentAmount)
        {
            RepairObject = repairObject;
            DurabilityPercent = durabilityPercent;
            PaymentCurrency = paymentCurrency;
            PaymentAmount = paymentAmount;
        }

        public WorldObject RepairObject { get; private set; }
        public float DurabilityPercent { get; private set; }
        public Currency PaymentCurrency { get; private set; }
        public float PaymentAmount { get; private set; }
    }
}
