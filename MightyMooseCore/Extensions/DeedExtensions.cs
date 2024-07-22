using Eco.Gameplay.Components;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Property;
using Eco.Shared.Utils;
using Eco.Moose.Data.Constants;

namespace Eco.Moose.Extensions
{
    public static partial class Extensions
    {
        public static int GetTotalPlotSize(this Deed deed) => deed.Plots.Count() * Constants.PLOT_SIZE_M2;

        public static bool IsVehicle(this Deed deed) => deed.OwnedObjects.Select(handle => handle.OwnedObject).OfType<WorldObject>().Any(x => x?.HasComponent<VehicleComponent>() == true);

        public static VehicleComponent GetVehicle(this Deed deed) => deed.OwnedObjects.Select(handle => handle.OwnedObject).OfType<WorldObject>().Where(x => x?.HasComponent<VehicleComponent>() == true).FirstOrDefault().GetComponent<VehicleComponent>();
    }
}
