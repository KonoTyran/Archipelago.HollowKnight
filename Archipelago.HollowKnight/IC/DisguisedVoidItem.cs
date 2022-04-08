using ItemChanger;
using ItemChanger.Tags;

namespace Archipelago.HollowKnight.IC
{
    internal class DisguisedVoidItem : AbstractItem
    {
        private string container;

        public DisguisedVoidItem(AbstractItem originalItem, string targetSlotName = null, string container = null)
        {
            this.container = container;
            name = originalItem.name;
            UIDef = new ArchipelagoUIDef(originalItem.UIDef, targetSlotName);

            InteropTag tag = AddTag<InteropTag>();
            tag.Message = "RecentItems";
            if (!string.IsNullOrEmpty(targetSlotName))
            {
                tag.Properties["DisplayMessage"] = $"{originalItem.GetPreviewName()}\nsent to {targetSlotName}.";
            }
            else
            {
                tag.Properties["DisplayMessage"] = $"{originalItem.GetPreviewName()}\nsent to the multiworld.";
            }
        }

        public override string GetPreferredContainer()
        {
            return container ?? "Unknown";
        }

        public override void GiveImmediate(GiveInfo info)
        {
            
        }
    }
}
