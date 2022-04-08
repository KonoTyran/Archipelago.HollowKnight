using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItemChanger;
using ItemChanger.Items;
using ItemChanger.Locations;

namespace Archipelago.HollowKnight.IC.Disguises
{
    internal class ContainerPreservingLocationDisguise : ILocationDisguise
    {
        public bool CanHandleLocation(AbstractLocation location)
        {
            return IsGrubLocation(location) || IsGeoRockLocation(location);
        }

        private bool IsGeoRockLocation(AbstractLocation location)
        {
            return location is ObjectLocation && location.name.StartsWith("Geo_Rock");
        }

        private bool IsGrubLocation(AbstractLocation location)
        {
            return location is ObjectLocation && location.name.StartsWith("Grub-") && !location.name.EndsWith("_Mimic");
        }

        public AbstractItem ProduceDisguisedItemForLocation(AbstractLocation location, AbstractItem originalItem)
        {
            var item = originalItem.Clone();
            item.ModifyItem += (args) =>
            {
                args.Item = VoidItem.Nothing;
                args.Info.FlingType = FlingType.DirectDeposit;
            };

            return item;
        }
    }
}
