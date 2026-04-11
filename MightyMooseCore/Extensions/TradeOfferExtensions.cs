using Eco.Gameplay.Components;

namespace Eco.Moose.Extensions
{
    public static partial class Extensions
    {
        public static string GetOfferContentName(this TradeOffer offer) => offer.IsTagOffer ? offer.Tag.MarkedUpName : offer.Stack.Item.MarkedUpName;
    }
}
