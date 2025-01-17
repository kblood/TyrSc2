﻿using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class PvZZealotArchon : Build
    {
        private Point2D OverrideDefenseTarget;
        private WallInCreator WallIn;
        private FallBackController FallBackController = new FallBackController() { ReturnFire = true };
        StutterForwardController StutterForwardController = new StutterForwardController() { MaxDist = 3 };
        private Point2D ShieldBatteryPos;
        private bool MassZerglings = false;
        private bool AggressiveZerglings;

        public override string Name()
        {
            return "PvZZealotArchon";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            ArmyObserverTask.Enable();
            ObserverScoutTask.Enable();
            if (Tyr.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Tyr.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ArchonMergeTask.Enable();
            ForwardProbeTask.Enable();
            ShieldRegenTask.Enable();
            HodorTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(StutterForwardController);
            MicroControllers.Add(FallBackController);
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new AdvanceController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.CreateNatural(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.GATEWAY, UnitTypes.ZEALOT, UnitTypes.GATEWAY});
                ShieldBatteryPos = DetermineShieldBatteryPos();
                WallIn.ReserveSpace();
            }

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0
                && (Count(UnitTypes.CYBERNETICS_CORE) > 0 || tyr.EnemyStrategyAnalyzer.EarlyPool)
                && (Count(UnitTypes.GATEWAY) >= 2 || !tyr.EnemyStrategyAnalyzer.EarlyPool));
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuild();
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => { return !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool; });
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.If(() => Count(UnitTypes.STALKER) < 5 || Count(UnitTypes.IMMORTAL) < 2 || Count(UnitTypes.NEXUS) >= 2);
            result.If(() => !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool || Count(UnitTypes.ZEALOT) < 8 || Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.TEMPEST);
            result.Train(UnitTypes.IMMORTAL, 3);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.IMMORTAL, 10);
            result.Train(UnitTypes.HIGH_TEMPLAR, () => Gas() >= 100);
            result.Train(UnitTypes.ZEALOT, 6);
            //result.Train(UnitTypes.STALKER, () => TotalEnemyCount(UnitTypes.ROACH) >= 5);
            result.Train(UnitTypes.ZEALOT);
            
            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural, WallIn.Wall[4].Pos, true);
            result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[3].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool);
            result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[1].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool);
            result.Building(UnitTypes.NEXUS, () => !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool || Completed(UnitTypes.ADEPT) + Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.STALKER) >= 8);
            result.Building(UnitTypes.CYBERNETICS_CORE, Natural, WallIn.Wall[1].Pos, true, () => !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool);
            result.Building(UnitTypes.CYBERNETICS_CORE, () => Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool && Count(UnitTypes.ZEALOT) >= 6);
            result.Building(UnitTypes.ASSIMILATOR, () => !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool || Count(UnitTypes.ZEALOT) >= 6);
            result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[3].Pos, true, () => !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool);
            if (ShieldBatteryPos == null)
                result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos);
            else
                result.Building(UnitTypes.SHIELD_BATTERY, Natural, ShieldBatteryPos, true);
            result.Building(UnitTypes.FORGE, () => Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool || AggressiveZerglings);
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 2, () => Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool || AggressiveZerglings);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Upgrade(UpgradeType.WarpGate);
            result.Building(UnitTypes.PYLON, Natural, () => Count(UnitTypes.CYBERNETICS_CORE) > 0 && Natural.ResourceCenter != null);
            result.Building(UnitTypes.TEMPLAR_ARCHIVE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FORGE, () => !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.GATEWAY, 2);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY, 2);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            //result.Building(UnitTypes.ASSIMILATOR, 3, () => TotalEnemyCount(UnitTypes.ROACH) >= 5);
            //result.Building(UnitTypes.ROBOTICS_FACILITY, () => TotalEnemyCount(UnitTypes.ROACH) >= 5);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 4, () => Minerals() >= 700 && Gas() < 200);
            result.If(() => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.COLOSUS) + Completed(UnitTypes.ARCHON) >= 3 && Count(UnitTypes.STALKER) + Completed(UnitTypes.ZEALOT) >= 10);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.COLOSUS) + Count(UnitTypes.ARCHON) + Count(UnitTypes.ADEPT) + Count(UnitTypes.ZEALOT) >= 15);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Completed(UnitTypes.STALKER) + Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.COLOSUS) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.ARCHON) + Completed(UnitTypes.DISRUPTOR) + Completed(UnitTypes.TEMPEST) + Count(UnitTypes.ADEPT) >= 40 || Minerals() >= 650);
            result.Building(UnitTypes.NEXUS);

            return result;
        }
        
        private Point2D DetermineShieldBatteryPos()
        {
            Point2D pos = SC2Util.TowardCardinal(WallIn.Wall[4].Pos, Natural.BaseLocation.Pos, 2);
            if (Tyr.Bot.buildingPlacer.CheckPlacement(pos, SC2Util.Point(2, 2), UnitTypes.PYLON, null, true))
                return pos;
            return null;
        }

        public override void OnFrame(Tyr tyr)
        {
            BalanceGas();
            
            ArchonMergeTask.Task.MergePos = OverrideDefenseTarget;

            FallBackController.Stopped = true;
            StutterForwardController.Stopped = Count(UnitTypes.NEXUS) >= 3 || TimingAttackTask.Task.Units.Count > 0 || Completed(UnitTypes.ZEALOT) > 0;


            if (!AggressiveZerglings && tyr.Frame <= 22.4 * 60 * 7)
            {
                int closeEnemyZerglingCount = 0;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.ZERGLING)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 50 * 50)
                        closeEnemyZerglingCount++;
                }
                if (AggressiveZerglings)
                    tyr.DrawText("Close zerglings: " + closeEnemyZerglingCount);
                if (closeEnemyZerglingCount >= 20)
                    AggressiveZerglings = true;
            }
            if (AggressiveZerglings)
                tyr.DrawText("Aggressive zerglings.");

            int wallDone = 0;
            foreach (WallBuilding building in WallIn.Wall)
            {
                if (!BuildingType.LookUp.ContainsKey(building.Type))
                {
                    wallDone++;
                    continue;
                }
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    if (agent.DistanceSq(building.Pos) <= 1 * 1)
                    {
                        wallDone++;
                        break;
                    }
                }
            }
            tyr.DrawText("Wall count: " + wallDone);
            HodorTask.Task.Stopped = Count(UnitTypes.NEXUS) >= 3 
                || TimingAttackTask.Task.Units.Count > 0 
                || (tyr.EnemyStrategyAnalyzer.EarlyPool && Completed(UnitTypes.ZEALOT) >= 2)
                || wallDone < WallIn.Wall.Count;
            if (HodorTask.Task.Stopped)
                HodorTask.Task.Clear();

            if (tyr.EnemyStrategyAnalyzer.EarlyPool || tyr.Frame >= 1800)
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            HodorTask.Task.Target = WallIn.Wall[2].Pos;

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 3 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Natural.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }

            if (!MassZerglings
                && !tyr.EnemyStrategyAnalyzer.EarlyPool
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 60
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ROACH) == 0
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.HYDRALISK) == 0)
            {
                MassZerglings = true;
                TimingAttackTask.Task.Clear();
            }
            tyr.DrawText("Zergling count: " + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING));

            //WorkerScoutTask.Task.StartFrame = 600;
            ObserverScoutTask.Task.Priority = 6;

            ForwardProbeTask.Task.Stopped = Completed(UnitTypes.STALKER) + Count(UnitTypes.ADEPT) + Count(UnitTypes.IMMORTAL) < 12;

            if (ForwardProbeTask.Task.Stopped)
                ForwardProbeTask.Task.Clear();


            if (tyr.EnemyStrategyAnalyzer.EarlyPool)
            {
                tyr.NexusAbilityManager.Stopped = Count(UnitTypes.ZEALOT) == 0;
                tyr.NexusAbilityManager.PriotitizedAbilities.Add(916);
            }
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);
            
            if (UpgradeType.LookUp[UpgradeType.ProtossGroundWeapons1].Done())
                TimingAttackTask.Task.RequiredSize = 24;
            else
                TimingAttackTask.Task.RequiredSize = 40;
            TimingAttackTask.Task.RetreatSize = 0;
            
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) >= 5;
            
            
            if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = NaturalDefensePos;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = tyr.EnemyStrategyAnalyzer.EarlyPool ? 50 : 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (Count(UnitTypes.PROBE) >= 24
                && Count(UnitTypes.NEXUS) < 2
                && Minerals() < 450)
                return;
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < Math.Min(70, 20 * Completed(UnitTypes.NEXUS))
                && (Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.PROBE) < 18 + 2 * Completed(UnitTypes.ASSIMILATOR)))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_BAY)
            {
                if (Minerals() >= 150
                    && Gas() >= 150
                    && !Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(50)
                    && Count(UnitTypes.COLOSUS) > 0)
                {
                    agent.Order(1097);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {

                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(87)
                     && Minerals() >= 150
                     && Gas() >= 150
                    && Completed(UnitTypes.STALKER) > 0)
                    agent.Order(1593);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100
                    && Completed(UnitTypes.ADEPT) > 0)
                    agent.Order(1594);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                         && Minerals() >= 100
                         && Gas() >= 100
                         && Completed(UnitTypes.ZEALOT) > 0)
                    agent.Order(1592);
            }
        }
    }
}
