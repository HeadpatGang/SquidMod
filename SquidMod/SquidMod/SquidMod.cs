using BepInEx;
using RoR2;
using R2API;
using R2API.Utils;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Linq;

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
        readonly List<BuffIndex> SquidBuffIndex = new List<BuffIndex>();
        readonly ItemIndex[] SquidItemIndex = { ItemIndex.InvadingDoppelganger };

        public static UnityEngine.Object Load(string path)
        {
            return Resources.Load(path, typeof(UnityEngine.Object));
        }

        public void Awake()
        {
            //Calls everything when the mod wakes up.
            ItemDefinition();
            JackedSquidsGettingBuffed();
            Hook();
        }

        private void ItemDefinition()
        {
            //Defines the variables needed to create the item.
            squidTurretItem = new ItemDef
            {
                name = "SQUID_TURRET_NAME",
                nameToken = "SQUID_TURRET_NAME",
                pickupToken = "SQUID_TURRET_PICKUP",
                descriptionToken = "SQUID_TURRET_DESC",
                loreToken = "SQUID_TURRET_LORE",
                tier = ItemTier.Lunar,
                pickupModelPath = "Prefabs/PickupModels/PickupSquidTurret",
                pickupIconPath = "Textures/ItemIcons/texSquidTurretIcon",
                //Remove is for scrappers/soup/shrine of order/3D printer
                canRemove = true,
                //Hidden is for, well, not being seen, like the BoostAttackSpeed.
                hidden = false,
            };
            var displayRules = new ItemDisplayRuleDict(null);
            AddTokens();
            ItemAPI.Add(new CustomItem(squidTurretItem, displayRules));
        }

        private void AddTokens()
        {
            //Adds all of our tokens previously made in ItemDefinition to an item in-game via the LanguageAPI
            R2API.LanguageAPI.Add("SQUID_TURRET_NAME", "Deadmans Friend");
            R2API.LanguageAPI.Add("SQUID_TURRET_PICKUP", "Spawns a Squid Polyp on Kill. <style=cIsUtility> 5% Chance </style> to become <style=cDeath> Hostile </style>");
            R2API.LanguageAPI.Add("SQUID_TURRET_DESC", "Killing an enemy spawns a Squid Polyp with a 1% chance to become <style=cArtifact> Elite </style>, but <style=cIsUtility> 5% Chance </style> <style=cStack> (+1% per stack) </style> to become <style=cDeath> Hostile </style>.");
            R2API.LanguageAPI.Add("SQUID_TURRET_LORE", "One squid in the ocean is worth a thousand on land.");
        }

        private void Hook()
        {
            //This is just a region of hooks, allows me to hook them here so the methods don't have them as that's ugly & dumb
            On.RoR2.GlobalEventManager.OnCharacterDeath += GalaticAquaticAquarium;
        }

        private void JackedSquidsGettingBuffed()
        {
            //Example of ugly, dumb hooking that's going on, but I think a small hook is fine being here.
            On.RoR2.Run.Start += (orig, self) =>
            {
                //Clears all the buffs in the index, then loops through all buffs in the Elite buff indice.
                //This allows the squids to get modded elite buffs aswell which is fucking stupid, but hey, memes.
                SquidBuffIndex.Clear();
                foreach (BuffIndex buff in BuffCatalog.eliteBuffIndices)
                {
                    SquidBuffIndex.Add(buff);
                };
                orig(self);
            };
        }

        private void GalaticAquaticAquarium(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport report)
        {
            //This is the entire spawning logic & by god it's a mess, but it works & I'll change it eventually, just not right now.
            //This starts off with if the attacker has an inventory, simple check for 90% of scenarios that enemies don't have inventories to prevent all deaths from being checked.
            if (report.attackerBody.inventory)
            {
                //Three variables are created, one for how many squidTurretItems the attacker has.
                //One for counting the chance of monster spawning, which is 5 + count of squidTurretItem in the attackers inventory.
                //Finally the squid team itself, 1/20 chance to become an enemy at base value, value increases for every stack the attacker has.
                int HowManySquidsAreInYourPocket = report.attackerBody.inventory.GetItemCount(squidTurretItem.itemIndex);
                int squidStackCheck = 5 + HowManySquidsAreInYourPocket;
                var squidTeam = Util.CheckRoll(squidStackCheck) ? TeamIndex.Monster : TeamIndex.Player;
                //Three if statements are created, one for checking if CharacterMaster is null, then do nothing.
                //One is created if DamageReport is null, then do nothing
                //One for if there's a victim and if there's an attacker, which is what starts the spawning itself.
                if (self is null) return;
                if (report is null) return;
                if (report.victimBody && report.attacker)
                {
                    //A check is then performed to see if the attackers count of squidTurretItem is greater than 0
                    if (HowManySquidsAreInYourPocket > 0)
                    {
                        //A spawn card is created utilizing the SquidTurret spawn card.
                        SpawnCard spawnCard = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscSquidTurret");
                        //A placement rule is created so that the Squid Turrets spawn on the victims body.
                        DirectorPlacementRule placementRule = new DirectorPlacementRule
                        {
                            spawnOnTarget = report.victimBody.transform,
                        };
                        //A spawn request is generated using the aforementioned variables of Spawn Card and the Placement Rule.
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, RoR2Application.rng)
                        {
                            teamIndexOverride = squidTeam
                        };
                        //A secondary spawn request is then made using the first one as a baseline
                        //The second spawn request delagates the initial spawn request into a new Action which is Spawn Result and names it result.
                        DirectorSpawnRequest directorSpawnRequest2 = directorSpawnRequest;
                        directorSpawnRequest2.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest2.onSpawnedServer, new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult result)
                        {
                        //A pseudo character named squidTurret which is the result of the spawned instance in the aforementioned directorSpawnRequest 2 and its Character Master is then inherited into it.
                        //The squid is then given 15 stacks of Health Decay (Half of a regular squid)
                        //It is also given 20 BoostAttackSpeeds which is then amplified by the amount of squidTurretItems the attacker has (Double the regular squid)
                        CharacterMaster squidTurret = result.spawnedInstance.GetComponent<CharacterMaster>();
                        squidTurret.inventory.GiveItem(ItemIndex.HealthDecay, 15);
                        squidTurret.inventory.GiveItem(ItemIndex.BoostAttackSpeed, 20 * HowManySquidsAreInYourPocket);
                        //A check is them performed to check if the squid is on the player index.
                        //If this check if passed, a 1/100 roll is performed to see if the squid inherits a random item from the SquidItemIndex created at the top of page.
                        //If this fails, a secondary 1/100 roll is performed to see if the squid inherits a random elite buff from the SquidBuffIndex created in "JackedSquidsGettingBuffed" method.
                            if (squidTeam == TeamIndex.Player)
                            {
                                if (Util.CheckRoll(1))
                                {
                                    squidTurret.inventory.GiveItem(SquidItemIndex[UnityEngine.Random.Range(0, SquidItemIndex.Count())]);
                                }
                                else if (Util.CheckRoll(1))
                                {
                                    squidTurret.GetBody().AddBuff(SquidBuffIndex[UnityEngine.Random.Range(0, SquidBuffIndex.Count())]);
                                }
                            }
                        //Once these checks have passed, the squidTurrets ownership is passed onto the attacker who initially spawned it.
                        squidTurret.minionOwnership.SetOwner(report.attackerMaster);
                        }));
                        //Finally the squid is attempted to be spawned.
                        DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                    }
                }
            }
            //The damage report is returned to itself, the character deaths are returned to itself and finally globaleventmanger is returned to itself.
            orig(self, report);
        }

        private void SpawnASquid()
        {
            //This is used for testing and debugging, if F1 is pressed, it spawns a squid item next to the player.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(squidTurretItem.itemIndex), transform.position, transform.forward * 20f);
            };
        }

        public void Update()
        {
            SpawnASquid();
        }
    }   
}
