using Eco.Gameplay.Components;
using Eco.Gameplay.Objects;
using Eco.Shared.Utils;

namespace Eco.Moose.Extensions
{
    public static partial class Extensions
    {
        public static string GetTagStrippedName(this WorldObject worldObject) => worldObject.MarkedUpName.ToString().StripTags();
    }
}
