using BepInEx;
using RoR2;
using R2API;
using R2API.Utils;
using UnityEngine;
using System;

namespace SquidPatrol
{
    [BepInDependency("com.bepis.r2api")]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(ItemDropAPI), nameof(LanguageAPI))]
    [BepInPlugin("com.Jessica.SquidPatrol", "Squid Patrol", "1.0.0")]
    public class SquidPatrol : BaseUnityPlugin
    {
        private static ItemDef squidTurretItem;

        public void Awake()
        {
            squidTurretItem = new ItemDef
            {
                name = "SQUID_TURRET_NAME",
                nameToken = "SQUID_TURRET_NAME",
                pickupToken = "SQUID_TURRET_SPAWNER_PICKUP",
                descriptionToken = "SQUID_TURRET_DESC",
                loreToken = "SQUID_TURRET_LORE",
                tier = ItemTier.Tier3,
                pickupIconPath = "Textures/MiscIcons/texMysteryIcon",
                pickupModelPath = "Prefabs/PickupModels/PickupMystery",
                canRemove = true,
                hidden = false,
            };
            var displayRules = new ItemDisplayRuleDict(null);
            AddTokens();
            ItemAPI.Add(new CustomItem(squidTurretItem, displayRules));
            Hook();
        }

        private void AddTokens()
        {
            R2API.LanguageAPI.Add("SQUID_TURRET_NAME", "Deadmans Friend");
            R2API.LanguageAPI.Add("SQUID_TURRET_PICKUP", "Spawn a Squid on Kill, Change for Affix");
            R2API.LanguageAPI.Add("SQUID_TURRET_DESC", "Whenever you <style=cIsDamage> kill an enemy</style> you have a <style=cIsUtility>1%</style> chance to spawn a Squid with an Affix. <style=cIsUtility>100% Chance to spawn a Squid </style>");
            R2API.LanguageAPI.Add("SQUID_TURRET_LORE", "PLACEHOLDER_LORE_FOR_RETRO_TO_DECIDE_ON");
        }

        private void Hook()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += GalaticAquaticAquarium;
        }

        private void GalaticAquaticAquarium(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport report)
        {
            if (self is null) return;
            if (report is null) return;
            if (report.victimBody && report.attacker)
            {
                int squidCounter = report.attackerBody.inventory.GetItemCount(squidTurretItem.itemIndex);
                if (squidCounter > 0)
                {
                    SpawnCard spawnCard = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscSquidTurret");
                    DirectorPlacementRule placementRule = new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                        minDistance = 5f,
                        maxDistance = 25f,
                        spawnOnTarget = report.victimBody.transform,
                    };
                    DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, RoR2Application.rng)
                    {
                        teamIndexOverride = TeamIndex.Player
                    };
                    DirectorSpawnRequest directorSpawnRequest2 = directorSpawnRequest;
                    directorSpawnRequest2.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest2.onSpawnedServer, new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult result)
                    {
                        CharacterMaster squidTurret = result.spawnedInstance.GetComponent<CharacterMaster>();
                        squidTurret.inventory.GiveItem(ItemIndex.HealthDecay, 30);
                        squidTurret.inventory.GiveItem(ItemIndex.BoostAttackSpeed, 10 * squidCounter);
                        if (squidTurret && Util.CheckRoll(1))
                        {
                            squidTurret.inventory.SetEquipmentIndex(EquipmentIndex.AffixRed);
                        }
                        if (squidTurret && Util.CheckRoll(1))
                        {
                            squidTurret.inventory.SetEquipmentIndex(EquipmentIndex.AffixBlue);
                        }
                        if (squidTurret && Util.CheckRoll(1))
                        {
                            squidTurret.inventory.SetEquipmentIndex(EquipmentIndex.AffixWhite);
                        }
                        if (squidTurret && Util.CheckRoll(1))
                        {
                            squidTurret.inventory.SetEquipmentIndex(EquipmentIndex.AffixHaunted);
                        }
                        if (squidTurret && Util.CheckRoll(1))
                        {
                            squidTurret.inventory.SetEquipmentIndex(EquipmentIndex.AffixPoison);
                        }
                    }));

                    DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                }
            }
            orig(self, report);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                //Too lazy to write GiveItem Squid 1 everytime I load into a map
                PlayerCharacterMasterController[] pcmc = new PlayerCharacterMasterController[1];
                PlayerCharacterMasterController.instances.CopyTo(pcmc, 0);
                pcmc[0].master.inventory.GiveItem(squidTurretItem.itemIndex);
                pcmc[0].master.inventory.GiveItem(ItemIndex.ExtraLife);

            };
        }
    }
}
