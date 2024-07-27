using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.Money;
using Eco.Gameplay.Economy.Transfer;
using Eco.Gameplay.Players;

namespace Eco.Moose.Extensions
{
    public static partial class Extensions
    {
        public static bool IsWhitelisted(this User user) => UserManager.Config.UserPermission.WhiteList.Contains(user);
        public static bool IsBanned(this User user) => UserManager.Config.UserPermission.BlackList.Contains(user);
        public static bool IsMuted(this User user) => UserManager.Config.UserPermission.MuteList.Contains(user);
        public static bool HasSpecialization(this User user, Type specialization) => user.Skillset.GetSkill(specialization)?.Level > 0;

        public static double GetSecondsSinceLogin(this User user) => user.IsOnline ? Simulation.Time.WorldTime.Seconds - user.LoginTime : 0.0;
        public static double GetSecondsSinceLogout(this User user) => user.IsOnline ? 0.0 : Simulation.Time.WorldTime.Seconds - user.LogoutTime;
        public static double GetSecondsLeftUntilExhaustion(this User user) => user.ExhaustionMonitor?.RemainingPlaytime ?? -1.0;

        public static float GetTotalXPMultiplier(this User user) => user.GetNutritionXP() + user.GetHousingXP();
        public static float GetNutritionXP(this User user) => user.Stomach.NutrientSkillRate();
        public static float GetHousingXP(this User user) => user.ResidencyPropertyValue != null ? user.ResidencyPropertyValue.Value : 0.0f;

        public static float GetWealthInCurrency(this User user, Currency cur)
        {
            float wealth = 0.0f;
            foreach (var account in BankAccountUtils.GetNonGovernmentAccountsAccessibleToUser(user, cur))
            {
                float amount = account.GetCurrencyHoldingVal(cur, user);
                if (amount < Transfers.AlmostZero)
                    continue;

                wealth += amount;
            }
            return wealth;
        }
    }
}
