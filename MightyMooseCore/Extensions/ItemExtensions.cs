using Eco.Gameplay.Items;
using Eco.Shared.Utils;

namespace Eco.Moose.Extensions
{
    public static partial class Extensions
    {
        public static bool HasTagWithName(this Item item, string name) => item.Tags().Any(tag => tag.DisplayName.ToString().EqualsCaseInsensitive(name));
    }
}
