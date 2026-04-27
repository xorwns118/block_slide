using System.Collections.Generic;
using UnityEngine;

namespace GridShift.Gameplay
{
    public class RuleChecker
    {
        private readonly HashSet<Vector2Int> goals;

        public RuleChecker(IEnumerable<Vector2Int> goalPositions)
        {
            goals = goalPositions != null ? new HashSet<Vector2Int>(goalPositions) : new HashSet<Vector2Int>();
        }

        public bool IsCleared(IEnumerable<Vector2Int> boxPositions)
        {
            if (boxPositions == null || goals.Count == 0)
            {
                return false;
            }

            int boxCount = 0;
            foreach (Vector2Int boxPosition in boxPositions)
            {
                boxCount++;
                if (!goals.Contains(boxPosition))
                {
                    return false;
                }
            }

            return boxCount == goals.Count;
        }
    }
}
