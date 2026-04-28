using System.Collections.Generic;
using UnityEngine;

namespace GridShift.Gameplay
{
    public class LevelSolution
    {
        public bool IsSolved { get; }
        public int MoveCount { get; }
        public int PushCount { get; }
        public int ExploredStateCount { get; }
        public string Reason { get; }
        public List<Vector2Int> MoveDirections { get; }

        public LevelSolution(
            bool isSolved,
            int moveCount,
            int pushCount,
            int exploredStateCount,
            string reason,
            IEnumerable<Vector2Int> moveDirections)
        {
            IsSolved = isSolved;
            MoveCount = moveCount;
            PushCount = pushCount;
            ExploredStateCount = exploredStateCount;
            Reason = reason;
            MoveDirections = moveDirections != null ? new List<Vector2Int>(moveDirections) : new List<Vector2Int>();
        }

        public static LevelSolution Failed(string reason, int exploredStateCount)
        {
            return new LevelSolution(false, -1, -1, exploredStateCount, reason, null);
        }
    }
}
