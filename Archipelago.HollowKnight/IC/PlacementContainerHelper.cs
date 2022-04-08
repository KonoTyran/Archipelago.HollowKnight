using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Placements;

namespace Archipelago.HollowKnight.IC
{
    internal class PlacementContainerHelper
    {
        public static string GetContainerType(AbstractLocation loc)
        {
            if (TryProcessShortCircuits(loc, out var contain))
            {
                return contain;
            }

            switch (loc)
            {
                case ExistingContainerLocation existingContainerLocation:
                    try
                    {
                        var plc = existingContainerLocation.Wrap();
                        var scplc = plc as ISingleCostPlacement;
                        return ExistingContainerPlacement.ChooseContainerType(scplc, existingContainerLocation, plc.Items);
                    }
                    catch (NullReferenceException)
                    {
                        Archipelago.Instance.LogWarn($"Could not resolve container for location '{loc}' of type {nameof(ExistingContainerLocation)}. Defaulting to Unknown.");
                        return Container.Unknown;
                    }
                case ShopLocation:
                    return Container.Shop;
                case ObjectLocation objectLocation:
                    try
                    {
                        objectLocation.GetContainer(out var _, out var container);
                        return container;
                    }
                    catch (NullReferenceException)
                    {
                        Archipelago.Instance.LogWarn($"Could not resolve container for location '{loc}' of type {nameof(ObjectLocation)}. Defaulting to Unknown.");
                        return Container.Unknown;
                    }
                case EnemyFsmLocation enemyFsmLocation:
                    try
                    {
                        enemyFsmLocation.GetContainer(out var _, out var container);
                        return container;
                    }
                    catch (NullReferenceException)
                    {
                        Archipelago.Instance.LogWarn($"Could not resolve container for location '{loc}' of type {nameof(EnemyFsmLocation)}. Defaulting to Enemy.");
                        return Container.Enemy;
                    }
                case EnemyLocation enemyLocation:
                    try
                    {
                        enemyLocation.GetContainer(out var _, out var container);
                        return container;
                    }
                    catch (NullReferenceException)
                    {
                        Archipelago.Instance.LogWarn($"Could not resolve container for location '{loc}' of type {nameof(EnemyLocation)}. Defaulting to Enemy.");
                        return Container.Enemy;
                    }
                case ContainerLocation containerLocation:
                    try
                    {
                        containerLocation.GetContainer(out var _, out var container);
                        return container;
                    }
                    catch (NullReferenceException)
                    {
                        Archipelago.Instance.LogWarn($"Could not resolve container for location '{loc}' of type {nameof(ContainerLocation)}. Defaulting to Unknown.");
                        return Container.Unknown;
                    }
                default:
                    return Container.Unknown;
            }
        }

        private static bool TryProcessShortCircuits(AbstractLocation loc, out string container)
        {
            var name = loc.name;

            if (name.StartsWith("Soul_Totem"))
            { 
                container = Container.Totem;
                return true;
            }

            if (name.StartsWith("Geo_Rock"))
            {
                container = Container.GeoRock;
                return true;
            }

            if (name.StartsWith("Grub-") && !name.EndsWith("_Mimic"))
            {
                container = Container.GrubJar;
                return true;
            }

            if (name.StartsWith("Grub-") && name.EndsWith("_Mimic"))
            {
                container = Container.Mimic;
                return true;
            }

            container = Container.Unknown;
            return false;
        }
    }
}
