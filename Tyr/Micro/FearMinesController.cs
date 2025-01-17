﻿using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Micro
{
    public class FearMinesController : CustomController
    {   
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            Point2D retreatFrom = null;
            float dist = 10 * 10;

            foreach (UnitLocation enemy in Tyr.Bot.EnemyMineManager.Mines)
            {
                if (!Tyr.Bot.EnemyManager.LastSeenFrame.ContainsKey(enemy.Tag))
                    continue;
                float newDist = agent.DistanceSq(enemy.Pos);
                if (Tyr.Bot.Frame - Tyr.Bot.EnemyManager.LastSeenFrame[enemy.Tag] < 2)
                    continue;

                if (newDist < dist)
                {
                    retreatFrom = SC2Util.To2D(enemy.Pos);
                    dist = newDist;
                }
            }

            if (retreatFrom != null)
            {
                agent.Order(Abilities.MOVE, agent.From(retreatFrom, 4));
                return true;
            }

            return false;
        }
    }
}
