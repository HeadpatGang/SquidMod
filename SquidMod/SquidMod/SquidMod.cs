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
            ItemDefinition();
            JackedSquidsGettingBuffed();
            SpawnASquid();
            Hook();
        }

        private void ItemDefinition()
        {
            //Defines the item to be used later on
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
                canRemove = true,
                hidden = false,
            };
            var displayRules = new ItemDisplayRuleDict(null);
            AddTokens();
            ItemAPI.Add(new CustomItem(squidTurretItem, displayRules));
        }

        private void AddTokens()
        {
            R2API.LanguageAPI.Add("SQUID_TURRET_NAME", "Deadmans Friend");
            R2API.LanguageAPI.Add("SQUID_TURRET_PICKUP", "Spawns a Squid Polyp on Kill. <style=cIsUtility> 5% Chance </style> to become <style=cDeath> Hostile </style>");
            R2API.LanguageAPI.Add("SQUID_TURRET_DESC", "Killing an enemy spawns a Squid Polyp with a 1% chance to become <style=cArtifact> Elite </style>, but <style=cIsUtility> 5% Chance </style> <style=cStack> (+1% per stack) </style> to become <style=cDeath> Hostile </style>.");
            R2API.LanguageAPI.Add("SQUID_TURRET_LORE", "One squid in the ocean is worth a thousand on land.");
        }

        private void Hook()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += GalaticAquaticAquarium;
        }

        private void JackedSquidsGettingBuffed()
        {
            On.RoR2.Run.Start += (orig, self) =>
            {
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
            if (report.attackerBody.inventory)
            {
                int HowManySquidsAreInYourPocket = report.attackerBody.inventory.GetItemCount(squidTurretItem.itemIndex);
                int squidStackCheck = 5 + HowManySquidsAreInYourPocket;
                var squidTeam = Util.CheckRoll(squidStackCheck) ? TeamIndex.Monster : TeamIndex.Player;
                if (self is null) return;
                if (report is null) return;
                if (report.victimBody && report.attacker)
                {
                    if (HowManySquidsAreInYourPocket > 0)
                    {
                        SpawnCard spawnCard = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscSquidTurret");
                        DirectorPlacementRule placementRule = new DirectorPlacementRule
                        {
                            spawnOnTarget = report.victimBody.transform,
                        };
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, RoR2Application.rng)
                        {
                            
                        };
                        DirectorSpawnRequest directorSpawnRequest2 = directorSpawnRequest;
                        directorSpawnRequest2.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest2.onSpawnedServer, new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult result)
                        {
                        CharacterMaster squidTurret = result.spawnedInstance.GetComponent<CharacterMaster>();
                        squidTurret.inventory.GiveItem(ItemIndex.HealthDecay, 15);
                        squidTurret.inventory.GiveItem(ItemIndex.BoostAttackSpeed, 20 * HowManySquidsAreInYourPocket);
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
                        squidTurret.minionOwnership.SetOwner(report.attackerMaster);
                        }));
                        DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                    }
                }
            }
            orig(self, report);
        }

        private void SpawnASquid()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(squidTurretItem.itemIndex), transform.position, transform.forward * 20f);
            };
        }

        public void Update()
        {

        }
    }   
}
