
using Eco.Gameplay.Economy;

namespace Eco.Moose.Features
{
    public static class Economy
    {
        public static float CalculateActiveCurrencyCirculation(Currency currency, bool includeTreasuaries = false)
        {
            float circulationAmount = 0.0f;
            foreach ((BankAccount account, CurrencyHolding holding) in BankAccountManager.Obj.GetAccountsForCurrency(currency))
            {
                if (!includeTreasuaries && account.Settlement != null && account == account.Settlement.TreasuryBankAccount)
                    continue; // Ignore treasury accounts

                if(account.DualPermissions.AllUsers.Any(user => user.IsActive))
                {
                    circulationAmount += holding.Val;
                }
            }
            return circulationAmount;
        }
    }
}
