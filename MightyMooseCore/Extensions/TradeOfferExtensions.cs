using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Shared.Utils;

namespace Eco.Moose.Extensions
{
    public static partial class Extensions
    {
        public static string GetOfferContentName(this TradeOffer offer)
        {
            if (offer == null)
                return "Unknown Offer";

            if (offer.IsTagOffer)
            {
                string tagName = ExtensionHelpers.GetObjectName(offer.Tag);
                if (!string.IsNullOrWhiteSpace(tagName))
                    return tagName.StripTags();
            }

            string itemName = ExtensionHelpers.GetObjectName(offer.Stack.Item);
            if (!string.IsNullOrWhiteSpace(itemName))
                return itemName.StripTags();

            return "Unknown Offer";
        }
    }
}
