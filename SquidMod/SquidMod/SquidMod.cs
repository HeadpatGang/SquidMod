using BepInEx;
using RoR2;
using UnityEngine;
using System;

namespace Jessica
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Jessica.SquidPatrol", "Squid Patrol", "1.0.0")]
    public class TestMod : BaseUnityPlugin
    {
        public void Awake()
        {
            Chat.AddMessage("Squid Patrol successfully loaded");
            Hook();
            //ObjectiveStack();
        }

        /*Archving this for later, it gets fucking stupid once it ramps.
        public void ObjectiveStack()
        {
            On.EntityStates.Squid.SquidWeapon.FireSpine.OnEnter += (orig, self) =>
            {
                //Calls the controller to use this as a reference point to access CharachterMaster
                PlayerCharacterMasterController[] pcmc = new PlayerCharacterMasterController[1];
                PlayerCharacterMasterController.instances.CopyTo(pcmc, 0);
                //Uses the PlayerCharacterMasterController array above to access the master inventory (ours)
                pcmc[0].master.inventory.GiveItem(ItemIndex.Squid);
                //Gives the turret the firing ability back.
                orig(self);
            };
        }*/


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
                int squidCounter = report.attackerBody.inventory.GetItemCount(ItemIndex.Squid);
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
                        squidTurret.inventory.GiveItem(ItemIndex.Squid);
                        squidTurret.inventory.GiveItem(ItemIndex.HealthDecay, 50);
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

        public void AffixRNG()
        {

        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                //Too lazy to write GiveItem Squid 1 everytime I load into a map
                PlayerCharacterMasterController[] pcmc = new PlayerCharacterMasterController[1];
                PlayerCharacterMasterController.instances.CopyTo(pcmc, 0);
                pcmc[0].master.inventory.GiveItem(ItemIndex.Squid);
                pcmc[0].master.inventory.GiveItem(ItemIndex.ExtraLife);

            };
        }
    }
}
