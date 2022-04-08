using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItemChanger.Locations;

namespace Archipelago.HollowKnight.IC
{
    internal class VanillaPlacementLocation : AutoLocation
    {
        public VanillaPlacementLocation(string name)
        {
            this.name = name;
        }

        protected override void OnLoad()
        {
            
        }

        protected override void OnUnload()
        {
            
        }
    }
}
