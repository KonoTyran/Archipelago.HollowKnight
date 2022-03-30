﻿using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.HollowKnight.IC;
using Archipelago.HollowKnight.MC;
using Archipelago.HollowKnight.SlotData;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Internal;
using ItemChanger.Items;
using ItemChanger.UIDefs;
using Modding;
using UnityEngine;

namespace Archipelago.HollowKnight
{
    // Known Issues
    // TODO: ADD COMPLETION CONDITION
    // TODO: make vanilla placements not shiny because kono hates me
    // BUG:  loading a save and resuming a multi doesn't work
    // TODO: Charm Notch rando
    // TODO: Grimmkin flame rando, I guess?
    // TODO: Test cases: Items send and receive from: Grubfather, Seer, Shops, Chests, Lore tablets, Geo Rocks, Lifeblood cocoons, Shinies, Egg Shop, Soul totems
    // TODO: Test cases: AP forfeit and AP collect.
    // NOTE: Tolerances are used to "help" generation of the randomized game be more tolerant of not reaching a precise number of required resources
    //       Guarantee you can skip X resource with X being your tolerance.
    // TODO: Far future: put all AP settings into ModeMenu and dynamically generate a YAML (or something)
    // INFO: Known issue: Start Game button on Archipelago Mode Menu may appear off-center for certain aspect ratios. Oh well.
    // TODO: Save item pickup index for when loading, so I don't reapply all items.
    // TODO: Sly's key shop is apparently just available within his normal shop?
    // TODO: What items should be placed into the pool when egg shop is turned on?
    public partial class Archipelago : Mod, ILocalSettings<ConnectionDetails>
    {
        private readonly Version ArchipelagoProtocolVersion = new Version(0, 2, 6);

        internal static Archipelago Instance;
        internal static Sprite Sprite;
        internal static Sprite SmallSprite;
        internal static System.Random Random;

        internal SpriteManager spriteManager;
        internal ConnectionDetails ApSettings;
        internal bool ArchipelagoEnabled = false;
        internal ArchipelagoSession session;

        private Dictionary<string, AbstractPlacement> vanillaItemPlacements = new();
        private long seed = 0;
        private TimeSpan timeBetweenReceiveItem = TimeSpan.FromMilliseconds(500);
        private DateTime lastUpdate = DateTime.MinValue;
        private SlotOptions slotOptions;
        private int[] notchCosts;

        public override string GetVersion() => new Version(0, 0, 1).ToString();

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            base.Initialize();
            Log("Initializing");

            Instance = this;
            spriteManager = new SpriteManager(typeof(Archipelago).Assembly, "Archipelago.HollowKnight.Resources.");
            Sprite = spriteManager.GetSprite("Icon");
            SmallSprite = spriteManager.GetSprite("IconSmall");

            MenuChanger.ModeMenu.AddMode(new ArchipelagoModeMenuConstructor());

            ModHooks.SavegameLoadHook += ModHooks_SavegameLoadHook;
            Events.OnItemChangerUnhook += Events_OnItemChangerUnhook;
            ModHooks.HeroUpdateHook += ModHooks_HeroUpdateHook;

            Log("Initialized");
        }

        private void ModHooks_HeroUpdateHook()
        {
            if (!ArchipelagoEnabled)
            {
                return;
            }

            if (DateTime.Now - timeBetweenReceiveItem > lastUpdate && session.Items.Any())
            {
                ReceiveItem(session.Items.DequeueItem().Item);
            }
        }

        public void ConnectAndRandomize()
        {
            if (!ArchipelagoEnabled)
            {
                return;
            }

            ItemChangerMod.CreateSettingsProfile();

            ConnectToArchipelago();
            CreateItemPlacements();
            CreateVanillaItemPlacements();

            if (slotOptions.RandomCharmCosts != -1)
            {
                // TODO: Eventually would load up PlayerDataEditModule and alter charm costs once IC updates.
            }
        }

        private void ConnectToArchipelago()
        {
            session = ArchipelagoSessionFactory.CreateSession(ApSettings.ServerUrl, ApSettings.ServerPort);

            var loginResult = session.TryConnectAndLogin("Hollow Knight", ApSettings.SlotName, ArchipelagoProtocolVersion, ItemsHandlingFlags.AllItems, password: ApSettings.ServerPassword);

            if (loginResult is LoginFailure failure)
            {
                // TODO: Better error handling to come later.
                throw new Exception(string.Join(", ", failure.Errors));
            }
            else if (loginResult is LoginSuccessful success)
            {
                // Read slot data.
                seed = (long)success.SlotData["seed"];
                Random = new System.Random(Convert.ToInt32(seed));

                SpecialPlacementHandler.Random = Random;
                SpecialPlacementHandler.GrubFatherCosts = SlotDataExtract.ExtractObjectFromSlotData<Dictionary<string, int>>(success.SlotData["grub_costs"]);
                SpecialPlacementHandler.SeerCosts = SlotDataExtract.ExtractObjectFromSlotData<Dictionary<string, int>>(success.SlotData["essence_costs"]);
                SpecialPlacementHandler.EggCosts = SlotDataExtract.ExtractObjectFromSlotData<Dictionary<string, int>>(success.SlotData["egg_costs"]);

                slotOptions = SlotDataExtract.ExtractObjectFromSlotData<SlotOptions>(success.SlotData["options"]);
                notchCosts = SlotDataExtract.ExtractObjectFromSlotData<int[]>(success.SlotData["charm_costs"]);
            }
        }

        public void ReceiveItem(int id)
        {
            LogDebug($"Receiving item ID {id}");
            var name = session.Items.GetItemName(id);
            LogDebug($"Item name is {name}.");

            // TODO: implement essence and egg shops (possibly by auto granting location check with enough essence/eggs collected)
            if (vanillaItemPlacements.TryGetValue(name, out var placement))
            {
                LogDebug($"Found vanilla placement for {name}.");

                // TODO: Note this can be done in itemOnGive in DisguisedVoidItem (and might be better there too)
                placement.GiveAll(new GiveInfo()
                {
                    FlingType = FlingType.DirectDeposit,
                    Container = Container.Unknown,
                    MessageType = MessageType.None
                });
            }
            else
            {
                LogDebug($"Could not find vanilla placement for {name}.");
            }
        }

        private void CreateItemPlacements()
        {
            void ScoutCallback(LocationInfoPacket packet)
            {
                MenuChanger.ThreadSupport.BeginInvoke(() =>
                {
                    foreach (var item in packet.Locations)
                    {
                        // TODO: I can do player name in recent item display. Use: item.Player
                        var locationName = session.Locations.GetLocationNameFromId(item.Location);
                        var itemName = session.Items.GetItemName(item.Item);

                        PlaceItem(locationName, itemName, item.Item);
                    }
                });
            }

            // TODO: Perhaps wrap this in a coroutine and wait while it's not done?
            var locations = new List<long>(session.Locations.AllLocations);
            session.Locations.ScoutLocationsAsync(ScoutCallback, locations.ToArray());
        }

        public void PlaceItem(string location, string name, int apLocationId)
        {
            LogDebug($"[PlaceItem] Placing item {name} into {location} with ID {apLocationId}");
            var originalLocation = string.Copy(location);
            location = StripShopSuffix(location);
            AbstractLocation loc = Finder.GetLocation(location);
            // TODO: remove this when logic has properly been imported and this mod can handle all location names.
            if (loc == null)
            {
                LogDebug($"[PlaceItem] Location was null: Name: {location}.");
                return;
            }

            AbstractPlacement pmt = loc.Wrap();
            AbstractItem item;


            if (Finder.ItemNames.Contains(name))
            {
                // Since HK is a remote items game, I don't want the placement to actually do anything. The item will come from the server.
                var originalItem = Finder.GetItem(name);
                item = new DisguisedVoidItem(originalItem);
            }
            else
            {
                // If item doesn't belong to Hollow Knight, then it is a remote item for another game.
                item = new ArchipelagoItem(name);
            }

            item.OnGive += (x) =>
            {
                var id = session.Locations.GetLocationIdFromName("Hollow Knight", originalLocation);
                session.Locations.CompleteLocationChecks(id);
            };

            if (SpecialPlacementHandler.IsShopPlacement(location))
            {
                LogDebug($"[PlaceItem] Detected shop placement for location: {location}");
                SpecialPlacementHandler.PlaceShopItem(pmt, item);
            }
            else if (SpecialPlacementHandler.IsSeerPlacement(location))
            {
                LogDebug($"[PlaceItem] Detected seer placement for location: {location}.");
                SpecialPlacementHandler.PlaceSeerItem(originalLocation, pmt, item);
            }
            else if (SpecialPlacementHandler.IsEggShopPlacement(location))
            {
                LogDebug($"[PlaceItem] Detected egg shop placement for location: {location}.");
                SpecialPlacementHandler.PlaceEggShopItem(pmt, item);
            }
            else if (SpecialPlacementHandler.IsGrubfatherPlacement(location))
            {
                LogDebug($"[PlaceItem] Detected Grubfather placement for original location: {originalLocation}. Trimmed location: {location}");
                SpecialPlacementHandler.PlaceGrubfatherItem(originalLocation, pmt, item);
            }
            else
            {
                pmt.Add(item);
            }

            // Export all placements as IEnumerable and addplacements all at once
            ItemChangerMod.AddPlacements(pmt.Yield());
        }

        private string StripShopSuffix(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return null;
            }

            var names = new[]
            {
                LocationNames.Sly_Key, LocationNames.Sly, LocationNames.Iselda, LocationNames.Salubra,
                LocationNames.Leg_Eater, LocationNames.Egg_Shop, LocationNames.Seer, LocationNames.Grubfather
            };

            foreach (var name in names)
            {
                if (location.StartsWith(name))
                {
                    return location.Substring(0, name.Length);
                }
            }
            return location;
        }

        private void CreateVanillaItemPlacements()
        {
            var allItems = Finder.GetFullItemList().Where(kvp => kvp.Value is not CustomSkillItem).ToDictionary(x => x.Key, x => x.Value);
            foreach (var kvp in allItems)
            {
                LogDebug($"Creating ArchipelagoLocation for a vanilla placement: Name: {kvp.Key}, Item: {kvp.Value}");
                var name = kvp.Key;
                var item = kvp.Value;

                var apLocation = new ArchipelagoLocation("Vanilla_" + name);
                var placement = apLocation.Wrap();
                placement.Add(item);
                item.UIDef = new MsgUIDef()
                {
                    name = new BoxedString(item.UIDef.GetPreviewName()),
                    shopDesc = new BoxedString(item.UIDef.GetShopDesc()),
                    sprite = new BoxedSprite(item.UIDef.GetSprite())
                };

                // TODO: This recycling could possibly also reset obtained to false (and ensure WasEverObtained() returns false) so that RecentItemsDisplay
                // will show the item again if the placement is reused.
                item.OnGive += (x) => x.Item.RefreshObtained();

                vanillaItemPlacements.Add(name, placement);
            }

            ItemChangerMod.AddPlacements(vanillaItemPlacements.Values.ToList());
        }

        private void ModHooks_SavegameLoadHook(int obj)
        {
            if (ApSettings == default)
            {
                return;
            }

            ConnectToArchipelago();
            vanillaItemPlacements = RetrieveVanillaItemPlacementsFromSave();
        }

        //TODO: I don't think this works. I need to retireve the custom placements somehow. homothety suggested ItemChanger.Internal.Ref.Settings.Placements
        /* When loading an existing game:
         *      - Load my vanilla placements, this could be done with a ItemChanger Tag - would have their own Tag type
         *      - Load my DisguisedVoidItem placements, this could be done with tag (or override OnLoad)
         *      - Load my ArchipelagoItem placements, which could probably be done with the same tag as DisguisedVoidItem
        */
        private Dictionary<string, AbstractPlacement> RetrieveVanillaItemPlacementsFromSave()
        {
            var placements = new Dictionary<string, AbstractPlacement>();
            var allItems = Finder.GetFullItemList().Where(kvp => kvp.Value is not CustomSkillItem).Select(x => x.Key);
            foreach (var item in allItems)
            {
                var location = Finder.GetLocation($"Vanilla_{item}");
                if (location == null)
                {
                    LogDebug($"Could not find previous vanilla item placement for item name: {item}");
                    continue;
                }
                placements.Add(item, location.Wrap());
            }
            return placements;
        }

        private void Events_OnItemChangerUnhook()
        {
            DisconnectArchipelago();
            vanillaItemPlacements = null;
            SpecialPlacementHandler.SeerCosts = null;
            SpecialPlacementHandler.GrubFatherCosts = null;
            SpecialPlacementHandler.EggCosts = null;
        }

        private void DisconnectArchipelago()
        {
            if (session?.Socket != null && session.Socket.Connected)
            {
                session.Socket.DisconnectAsync();
            }

            session = null;
        }

        public void OnLoadLocal(ConnectionDetails details)
        {
            ApSettings = details;
        }

        public ConnectionDetails OnSaveLocal()
        {
            return ApSettings;
        }
    }
}