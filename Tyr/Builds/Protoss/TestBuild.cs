﻿using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.CombatSim;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class TestBuild : Build
    {
        public override string Name()
        {
            return "TestBuild";
        }

        public override void InitializeTasks()
        {
        }

        public override void OnStart(Tyr tyr)
        {
            /*
            FileUtil.Debug("Zealot: \n" + UnitTypes.LookUp[UnitTypes.ZEALOT]);
            FileUtil.Debug("Stalker: \n" + UnitTypes.LookUp[UnitTypes.STALKER]);
            FileUtil.Debug("Adept: \n" + UnitTypes.LookUp[UnitTypes.ADEPT]);
            FileUtil.Debug("Marine: \n" + UnitTypes.LookUp[UnitTypes.MARINE]);
            FileUtil.Debug("Marauder: \n" + UnitTypes.LookUp[UnitTypes.MARAUDER]);
            FileUtil.Debug("Thor: \n" + UnitTypes.LookUp[UnitTypes.THOR]);
            */
            TestCombatSim.Test();
        }

        float LastX;
        float LastY;
        float LastHealth;
        float LastShield;
        float LastEnergy;
        string buffs = "";

        public override void OnFrame(Tyr tyr)
        {
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.IMMORTAL)
                {
                    string newBuffs = "";
                    foreach (uint buff in agent.Unit.BuffIds)
                        newBuffs += buff + ", ";
                    if (//agent.Unit.Pos.X != LastX
                        //|| agent.Unit.Pos.Y != LastY ||
                        agent.Unit.Health != LastHealth
                        || agent.Unit.Shield != LastShield
                        || agent.Unit.Energy != LastEnergy
                        || newBuffs != buffs)
                    {
                        buffs = newBuffs;
                        FileUtil.Debug("Frame: " + tyr.Frame);
                        FileUtil.Debug("Distance travelled: " + Math.Sqrt(SC2Util.DistanceSq(new Point2D() { X = LastX, Y = LastY }, agent.Unit.Pos)));
                        LastX = agent.Unit.Pos.X;
                        LastY = agent.Unit.Pos.Y;
                        LastHealth = agent.Unit.Health;
                        LastShield = agent.Unit.Shield;
                        LastEnergy = agent.Unit.Energy;
                        FileUtil.Debug("X: " + LastX);
                        FileUtil.Debug("Y: " + LastY);
                        FileUtil.Debug("Health: " + LastHealth);
                        FileUtil.Debug("Shield: " + LastShield);
                        FileUtil.Debug("Energy: " + LastEnergy);
                        FileUtil.Debug("Buffs: " + buffs);
                        FileUtil.Debug("");
                    }
                }
            }
        }
    }
}
