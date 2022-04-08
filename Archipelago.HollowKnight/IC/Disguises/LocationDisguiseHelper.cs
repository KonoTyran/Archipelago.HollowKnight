using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItemChanger;
using ItemChanger.Tags;

namespace Archipelago.HollowKnight.IC.Disguises
{
    internal static class LocationDisguiseHelper
    {
        public static void ApplyArchipelagoUIDefAndInteropTags(AbstractItem item, string targetSlotName)
        {
            ApplyArchipelagoUIDefAndInteropTags(item, item, targetSlotName);
        }

        public static void ApplyArchipelagoUIDefAndInteropTags(AbstractItem item, AbstractItem disguiseItem, string targetSlotName)
        {
            item.UIDef = new ArchipelagoUIDef(disguiseItem.UIDef, targetSlotName);

            InteropTag tag = item.AddTag<InteropTag>();
            tag.Message = "RecentItems";
            if (!string.IsNullOrEmpty(targetSlotName))
            {
                tag.Properties["DisplayMessage"] = $"{item.UIDef.GetPreviewName()}\nsent to {targetSlotName}.";
            }
            else
            {
                tag.Properties["DisplayMessage"] = $"{item.UIDef.GetPreviewName()}\nsent to the multiworld.";
            }
        }
    }
}
