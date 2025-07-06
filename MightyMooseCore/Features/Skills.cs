using Eco.Gameplay.Players;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Skills;
using Eco.Moose.Utils.Lookups;
using static Eco.Moose.Data.CommandData;

namespace Eco.Moose.Features
{
    public static class Skills
    {
        public static SpecialtyAssignmentLookupResult LookupSpecialtyAssignments(bool includeInactive, bool includeScrollNoStar = false, bool includeNonRefundable = false, Settlement? settlementFilter = null)
        {
            List<Skill> specialties = includeNonRefundable ? Lookups.Specialties.ToList() : Lookups.RefundableSpecialties.ToList();
            Dictionary<Skill, List<User>> playersPerSpecialty = new Dictionary<Skill, List<User>>();
            foreach (Skill skill in specialties)
            {
                playersPerSpecialty.Add(skill, new List<User>());
            }

            IEnumerable<User>? userList = settlementFilter == null ? Lookups.Users : settlementFilter.Citizens;
            if (userList != null)
            {
                foreach (User user in userList)
                {
                    // Conditionally filter out inactive players
                    if (!includeInactive && !user.IsActive)
                        continue;

                    foreach (Skill skill in user.Skillset.Skills)
                    {
                        // Find the skill in our list that matches the type of the skill in the skillset
                        Skill? matchingSkill = specialties.FirstOrDefault(s => s.GetType() == skill.GetType());
                        if (matchingSkill == null)
                            continue;

                        // Conditionally ignore players who have only read the scroll but not consumed a star
                        if (skill.StarsSpent < 1 && (!includeScrollNoStar || skill.TimeLearned == double.MaxValue)) // The TimeLearned check is for determining if a skill is a starting skill
                            continue;

                        playersPerSpecialty[matchingSkill].Add(user);
                    }
                }
            }

            return new SpecialtyAssignmentLookupResult(includeInactive, includeScrollNoStar, includeNonRefundable, settlementFilter, specialties, playersPerSpecialty);
        }
    }
}
