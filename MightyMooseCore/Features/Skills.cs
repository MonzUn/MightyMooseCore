using Eco.Gameplay.Players;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Skills;
using Eco.Moose.Utils.Lookups;
using static Eco.Moose.Data.CommandData;

namespace Eco.Moose.Features
{
    public static class Skills
    {
        public static SpecialtyAssignmentData GetPlayerSpecialtyData(Settlement? settlementFilter, bool includeNonRefundable = false, bool includeLevelZero = false)
        {
            List<Skill> specialties = includeNonRefundable ? Lookups.Specialties.ToList() : Lookups.RefundableSpecialties.ToList();
            Dictionary<Skill, List<User>> allPlayersPerSpecialty = new Dictionary<Skill, List<User>>();
            Dictionary<Skill, List<User>> activePlayersPerSpecialty = new Dictionary<Skill, List<User>>();
            foreach (Skill skill in specialties)
            {
                allPlayersPerSpecialty.Add(skill, new List<User>());
                activePlayersPerSpecialty.Add(skill, new List<User>());
            }

            IEnumerable<User>? userList = settlementFilter == null ? Lookups.Users : settlementFilter.Citizens;
            if (userList != null)
            {
                foreach (User user in userList)
                {
                    foreach (Skill skill in user.Skillset.Skills)
                    {
                        // Find the skill in our list that matches the type of the skill in the skillset
                        Skill? matchingSkill = specialties.FirstOrDefault(s => s.GetType() == skill.GetType());
                        if (matchingSkill == null)
                            continue;

                        if (!includeLevelZero && skill.Level < 1) // Conditionally ignore players who have only read the scroll but not consumed a star
                            continue;

                        allPlayersPerSpecialty[matchingSkill].Add(user);
                        if (user.IsActive)
                            activePlayersPerSpecialty[matchingSkill].Add(user);
                    }
                }
            }

            return new SpecialtyAssignmentData(settlementFilter, specialties, allPlayersPerSpecialty, activePlayersPerSpecialty);
        }
    }
}
