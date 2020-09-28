using BepInEx;
using RoR2;
using R2API;
using R2API.Utils;
using UnityEngine;
using System;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SquidPatrol
{

    [BepInDependency("com.bepis.r2api")]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(ItemDropAPI), nameof(LanguageAPI))]
    [BepInPlugin("com.Jessica.SquidPatrol", "Squid Patrol", "1.0.0")]
    public class SquidPatrol : BaseUnityPlugin
    {
        private static ItemDef squidTurretItem;
        readonly EquipmentIndex[] AffixIndex = { EquipmentIndex.AffixRed, EquipmentIndex.AffixBlue, EquipmentIndex.AffixWhite, EquipmentIndex.AffixHaunted, EquipmentIndex.AffixPoison };
        readonly ItemIndex[] SquidIndex = { ItemIndex.InvadingDoppelganger };

        public static UnityEngine.Object Load(string path)
        {
            return Resources.Load(path, typeof(UnityEngine.Object));
        }

        public void Awake()
        {
            //Calls all of my methods
            ItemDefinition();
            Hook();
        }

        private void ItemDefinition()
        {
            //Defines the item to be used later on
            squidTurretItem = new ItemDef
            {
                //R2 API Tokens
                name = "SQUID_TURRET_NAME",
                nameToken = "SQUID_TURRET_NAME",
                pickupToken = "SQUID_TURRET_PICKUP",
                descriptionToken = "SQUID_TURRET_DESC",
                loreToken = "SQUID_TURRET_LORE",
                //Makes it Red Tier
                tier = ItemTier.Lunar,
                //Default Squid stuff at the moment, custom stuff soonTM?
                pickupModelPath = "Prefabs/PickupModels/PickupSquidTurret",
                pickupIconPath = "Textures/ItemIcons/texSquidTurretIcon",
                //Can be removed by 3D Printer / Shrine of Order / Scrapper
                canRemove = true,
                //Isn't hidden, E.G AttackBoost, DrizzlePlayerHelper. Can be seen in the inventory
                hidden = false,
            };
            var displayRules = new ItemDisplayRuleDict(null);
            AddTokens();
            ItemAPI.Add(new CustomItem(squidTurretItem, displayRules));
        }

        private void AddTokens()
        {
            //All of our previously defined tokens are being added onto the item.
            R2API.LanguageAPI.Add("SQUID_TURRET_NAME", "Deadmans Friend");
            R2API.LanguageAPI.Add("SQUID_TURRET_PICKUP", "Spawn a Squid on Kill");
            R2API.LanguageAPI.Add("SQUID_TURRET_DESC", "Killing an enemy spawns a Squid <style=cIsUtility> 5% Chance </style> to become <style=cDeath> Hostile </style> <style=cStack> -1% per Stack </style> \n <style=cArtifact> 1% Chance for an Affix </style>.(Blazing, Glacial, Overloading, Malachite, Celestial, Umbral)");
            R2API.LanguageAPI.Add("SQUID_TURRET_LORE", "One squid in the ocean is worth a thousand on land.");
        }

        private void Hook()
        {
            //All of my hooks are going here because it's nicer to look at & easier to configure / update.
            On.RoR2.GlobalEventManager.OnCharacterDeath += GalaticAquaticAquarium;
        }

        private void GalaticAquaticAquarium(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport report)
        {
            if (report.attackerBody.inventory)
            {
                int squidCheck = 5 - report.attackerBody.inventory.GetItemCount(squidTurretItem.itemIndex);
                var squidTeam = Util.CheckRoll(squidCheck) ? TeamIndex.Monster : TeamIndex.Player;
                //If I am null or the report is null, do nothing.
                if (self is null) return;
                if (report is null) return;
                //If there's a victim & an attacker start the spawning process (basically on kill)
                if (report.victimBody && report.attacker)
                {
                    //Start a counter for how many Deadman Friend's are in the players inventory. (rename to deadmanCounter maybe?)
                 int HowManySquidsAreInYourPocket = report.attackerBody.inventory.GetItemCount(squidTurretItem.itemIndex);
                    if (HowManySquidsAreInYourPocket > 0)
                    {
                        //Spawn card is just what's being spawned, in this case it's a Squid Turret
                        SpawnCard spawnCard = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscSquidTurret");
                        //Placement rule is how far from the body the squid can spawn
                        DirectorPlacementRule placementRule = new DirectorPlacementRule
                        {
                            placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                            minDistance = 5f,
                            maxDistance = 25f,
                            spawnOnTarget = report.victimBody.transform,
                        };
                        //Starts the spawning request using everything created so far
                        //Spawn card for what's being spawned, PlacementRule for where it's being spawned.
                        //Index is set to player so it doesn't kill the player.
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, RoR2Application.rng)
                        {
                            teamIndexOverride = squidTeam
                        };
                        //Creates a secondary spawn request that mimics the first ones properties, just this time adding items into the spawn request.
                        //Can potentially be brought down into one spawn request.
                        DirectorSpawnRequest directorSpawnRequest2 = directorSpawnRequest;
                        directorSpawnRequest2.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest2.onSpawnedServer, new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult result)
                        {
                            //Gets the squids inventory from the spawned instance that had occured
                            //Gives the squids health decay in order to drains its hp & the attack speed boost to speeds its attack up per stack.
                            CharacterMaster squidTurret = result.spawnedInstance.GetComponent<CharacterMaster>();
                            squidTurret.inventory.GiveItem(ItemIndex.HealthDecay, 30);
                            squidTurret.inventory.GiveItem(ItemIndex.BoostAttackSpeed, 10 * HowManySquidsAreInYourPocket);
                            if (Util.CheckRoll(1))
                            {
                                squidTurret.inventory.GiveItem(SquidIndex[UnityEngine.Random.Range(0, 2)]);
                            }
                            if (Util.CheckRoll(1))
                            {
                                squidTurret.inventory.SetEquipmentIndex(AffixIndex[UnityEngine.Random.Range(0, 5)]);
                            }
                        }));
                        //Finally, sending the reuqest to spawn the squid with everything so far.
                        DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                    }
                }
            }
            //Since it's a hook, it needs to be restored to its original point & that's done here.
            orig(self, report);
        }

        public void Update()
        {
            //Very self explanatory, if the F1 key is pressed, create an item drop infront of the player with the Deadmans Friend item.
            //Used for testing models / pickup models without needing to find one.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(squidTurretItem.itemIndex), transform.position, transform.forward * 20f);
            };
        }
   
    }
}
