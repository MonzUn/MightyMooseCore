using Eco.Gameplay.Players;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Skills;

namespace Eco.Moose.Data
{
    public static class CommandData
    {
        public class SpecialtyAssignmentData
        {
            public SpecialtyAssignmentData(Settlement? settlement, IEnumerable<Skill> specialties, Dictionary<Skill, List<User>> allPlayersPerSpecialty, Dictionary<Skill, List<User>> activePlayersPerSpecialty)
            {
                Settlement = settlement;
                Specialties = specialties.ToList();
                AllPlayers = allPlayersPerSpecialty;
                ActivePlayers = activePlayersPerSpecialty;

                TotalPlayerCount = new Dictionary<Skill, int>();
                foreach (var entry in allPlayersPerSpecialty)
                {
                    TotalPlayerCount.Add(entry.Key, entry.Value.Count);
                }

                ActivePlayerCount = new Dictionary<Skill, int>();
                foreach (var entry in activePlayersPerSpecialty)
                {
                    ActivePlayerCount.Add(entry.Key, entry.Value.Count);
                }
            }

            public Settlement? Settlement { get; private set; } = null;
            public List<Skill> Specialties { get; private set; }
            public Dictionary<Skill, List<User>> AllPlayers { get; private set; }
            public Dictionary<Skill, List<User>> ActivePlayers { get; private set; }
            public Dictionary<Skill, int> TotalPlayerCount { get; private set; }
            public Dictionary<Skill, int> ActivePlayerCount { get; private set; }
        }
    }
}
