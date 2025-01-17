﻿using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ProxyTwoGateTask : Task
    {
        public static ProxyTwoGateTask Task = new ProxyTwoGateTask();
        public Point2D HideLocation;
        public bool BuildRobo = false;

        public List<BuildRequest> BuildRequests = new List<BuildRequest>();

        public int DesiredWorkers = 1;

        public List<uint> RequiredBuildings = new List<uint>();

        public ProxyTwoGateTask() : base(20)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (DesiredWorkers - Units.Count > 0 && GetHideLocation() != null)
                result.Add(new UnitDescriptor() { Pos = GetHideLocation(), Count = DesiredWorkers - Units.Count, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.Frame > 100;
        }

        public Point2D GetHideLocation()
        {
            if (HideLocation == null)
            {
                if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
                    return null;

                Point2D enemyMain = Tyr.Bot.TargetManager.PotentialEnemyStartLocations[0];
                Point2D enemyNatural = Tyr.Bot.MapAnalyzer.GetEnemyNatural().Pos;

                PotentialHelper potential = new PotentialHelper(enemyNatural, 30);
                potential.From(enemyMain);
                Point2D closeTo = potential.Get();
                
                float dist = 10000;
                foreach (Base b in Tyr.Bot.BaseManager.Bases)
                {
                    float newDist = SC2Util.DistanceSq(closeTo, b.BaseLocation.Pos);
                    
                    if (newDist >= dist)
                        continue;

                    if (SC2Util.DistanceSq(enemyMain, b.BaseLocation.Pos) < 4)
                        continue;
                    if (SC2Util.DistanceSq(enemyNatural, b.BaseLocation.Pos) < 4)
                        continue;
                    dist = newDist;
                    HideLocation = b.BaseLocation.Pos;
                }
                if (Tyr.Bot.EnemyRace == Race.Zerg)
                {
                    potential = new PotentialHelper(HideLocation, 15);
                    potential.To(Tyr.Bot.MapAnalyzer.StartLocation);
                    HideLocation = potential.Get();
                }
            }
            return HideLocation;
        }

        public override void OnFrame(Tyr tyr)
        {
            BuildingType pylonType = BuildingType.LookUp[UnitTypes.PYLON];
            BuildingType gatewayType = BuildingType.LookUp[UnitTypes.GATEWAY];
            Point2D hideLocation = GetHideLocation();
            if (hideLocation == null)
                return;
            Agent pylon = null;
            Agent gateway = null;
            int gatewayCount = 0;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType != UnitTypes.PYLON
                    && agent.Unit.UnitType != UnitTypes.GATEWAY
                    && agent.Unit.UnitType != UnitTypes.WARP_GATE)
                    continue;
                if (agent.DistanceSq(HideLocation) > 20 * 20)
                    continue;
                if (agent.Unit.UnitType == UnitTypes.PYLON)
                    pylon = agent;
                else if (agent.Unit.UnitType == UnitTypes.GATEWAY || agent.Unit.UnitType == UnitTypes.WARP_GATE)
                {
                    gatewayCount++;
                    gateway = agent;
                }

            }
            if (pylon == null && tyr.Minerals() >= 100 && BuildRequests.Count == 0)
            {
                Point2D placement = ProxyBuildingPlacer.FindPlacement(GetHideLocation(), pylonType.Size, UnitTypes.PYLON);
                if (placement != null)
                    BuildRequests.Add(new BuildRequest() { Type = UnitTypes.PYLON, Pos = placement });
            }
            else if (gatewayCount < 2 && pylon != null && pylon.Unit.BuildProgress > 0.99 && tyr.Minerals() >= 150)
            {
                Point2D placement = ProxyBuildingPlacer.FindPlacement(GetHideLocation(), gatewayType.Size, UnitTypes.GATEWAY);
                if (placement != null)
                    BuildRequests.Add(new BuildRequest() { Type = UnitTypes.GATEWAY, Pos = placement });
            }

            List<BuildRequest> doneRequests = new List<BuildRequest>();
            foreach (BuildRequest request in BuildRequests)
            {
                if (request.worker != null && !Tyr.Bot.UnitManager.Agents.ContainsKey(request.worker.Unit.Tag))
                    request.worker = null;
                if (request.worker == null)
                {
                    foreach(Agent agent in Units)
                    {
                        if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                            continue;
                        request.worker = agent;
                        break;
                    }
                }

                if (!ProxyBuildingPlacer.CheckPlacement(request.Pos, BuildingType.LookUp[request.Type].Size, request.Type, null, true))
                {
                    doneRequests.Add(request);
                    continue;
                }
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    if (agent.Unit.UnitType == request.Type
                        && agent.DistanceSq(request.Pos) < 4)
                    {
                        doneRequests.Add(request);
                        break;
                    }
                }
            }

            foreach (BuildRequest request in doneRequests)
                BuildRequests.Remove(request);
            
            foreach (Agent agent in Units)
            {
                bool building = false;
                foreach (BuildRequest request in BuildRequests)
                {
                    if (request.worker == null || request.worker.Unit.Tag != agent.Unit.Tag)
                        continue;

                    building = true;
                    if (agent.DistanceSq(request.Pos) <= 10 * 10)
                        agent.Order(BuildingType.LookUp[request.Type].Ability, request.Pos);
                    else
                        agent.Order(Abilities.MOVE, request.Pos);
                    break;
                }
                if (building)
                    continue;

                if (agent.DistanceSq(GetHideLocation()) >= 4 * 4)
                {
                    agent.Order(Abilities.MOVE, GetHideLocation());
                    continue;
                }
            }
        }
    }
}
