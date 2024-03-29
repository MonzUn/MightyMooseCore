﻿using Eco.Gameplay.Economy.Money;
using Eco.Gameplay.Economy.Transfer;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Players;
using Eco.Gameplay.Items;
using Eco.Gameplay.Components;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Property;
using Eco.Shared.Utils;

namespace Eco.Moose.Utils.Extensions
{
    using Constants = Constants.Constants;

    public static class Extensions
    {
        #region User

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

        #endregion

        #region Item

        public static bool HasTagWithName(this Item item, string name) => item.Tags().Any(tag => tag.DisplayName.ToString().EqualsCaseInsensitive(name));

        #endregion

        #region Deed

        public static int GetTotalPlotSize(this Deed deed) => deed.Plots.Count() * Constants.ECO_PLOT_SIZE_M2;

        public static bool IsVehicle(this Deed deed) => deed.OwnedObjects.Select(handle => handle.OwnedObject).OfType<WorldObject>().Any(x => x?.HasComponent<VehicleComponent>() == true);

        public static VehicleComponent GetVehicle(this Deed deed) => deed.OwnedObjects.Select(handle => handle.OwnedObject).OfType<WorldObject>().Where(x => x?.HasComponent<VehicleComponent>() == true).FirstOrDefault().GetComponent<VehicleComponent>();

        #endregion
    }
}
