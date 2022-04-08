using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItemChanger;

namespace Archipelago.HollowKnight.IC.Disguises
{
    internal interface ILocationDisguise
    {
        bool CanHandleLocation(AbstractLocation location);

        AbstractItem ProduceDisguisedItemForLocation(AbstractLocation location, AbstractItem originalItem);
    }
}
