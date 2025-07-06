using Eco.Gameplay.Aliases;
using Eco.Gameplay.Components;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Settlements;
using Eco.Moose.Data;
using Eco.Moose.Extensions;
using Eco.Moose.Utils.Lookups;

namespace Eco.Moose.Features
{
    public static class WorldObjects
    {
        public static RepairBountyLookupResult LookupRepairBounties(bool includeInactive, Settlement? settlementFilter = null, User? userFilter = null)
        {
            RepairBountyLookupResult result = new RepairBountyLookupResult(includeInactive, settlementFilter, userFilter);
            foreach (RepairBountyComponent bountyComponent in Lookups.ClaimableRepairBounties)
            {
                WorldObject worldObject = bountyComponent.Parent;
                PartsComponent partsComponent = worldObject.GetComponent<PartsComponent>();
                IAlias owner = worldObject.Owners;
                if (owner == null)
                    continue;

                // Filter by owner Active demographic
                if (!includeInactive && !owner.UserSet.Any(user => user.IsActive))
                    continue;

                // Filter by owner citizenship
                if (settlementFilter != null && !owner.UserSet.Any(user => user.AllCitizenships.Contains(settlementFilter)))
                    continue;

                // Filter by user skills
                if (userFilter != null && !partsComponent.SkillReqs.Any(skill => userFilter.HasSpecialization(skill.SkillType, skill.Level)))
                    continue;

                float durabilityPercent = partsComponent.TotalDurability() * 100;
                result.Bounties.Add(new RepairBountyLookupData(worldObject, durabilityPercent, bountyComponent.Currency, bountyComponent.ProratedPrice()));
            }

            return result;
        }
    }
}
