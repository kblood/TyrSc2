﻿using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class FearEnemyController : CustomController
    {
        private HashSet<uint> Scared = new HashSet<uint>();
        private HashSet<uint> Terror = new HashSet<uint>();
        public float Range;
        public int CourageCount = 30;
        public float EnemyBaseRange = 0;
        public bool DefendHome = true;

        public FearEnemyController(uint from, uint to, float range)
        {
            Scared.Add(from);
            Terror.Add(to);
            Range = range;
        }

        public FearEnemyController(uint from, HashSet<uint> to, float range)
        {
            Scared.Add(from);
            Terror = to;
            Range = range;
        }

        public FearEnemyController(HashSet<uint> from, uint to, float range)
        {
            Scared = from;
            Terror.Add(to);
            Range = range;
        }

        public FearEnemyController(HashSet<uint> from, HashSet<uint> to, float range)
        {
            Scared = from;
            Terror = to;
            Range = range;
        }

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (!Scared.Contains(agent.Unit.UnitType))
                return false;

            if (EnemyBaseRange > 0 && agent.DistanceSq(Tyr.Bot.TargetManager.PotentialEnemyStartLocations[0]) <= EnemyBaseRange * EnemyBaseRange)
                return false;

            int totalUnits = 0;
            foreach (uint type in Scared)
                totalUnits += Tyr.Bot.UnitManager.Completed(type);
            if (totalUnits >= CourageCount)
                return false;

            if (agent.DistanceSq(Tyr.Bot.MapAnalyzer.StartLocation) < 40 * 40 && DefendHome)
                return false;
            float dist;

            Point2D retreatFrom = null;
            dist = 15 * 15;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (!Terror.Contains(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    retreatFrom = SC2Util.To2D(enemy.Pos);
                    dist = newDist;
                }
            }
            if (retreatFrom != null && dist < Range * Range)
            {
                agent.Order(Abilities.MOVE, agent.From(retreatFrom, 4));
                return true;
            }

            return false;
        }
    }
}
