using System.Collections.Generic;
using UnityEngine;

namespace GridShift.Data
{
    [System.Serializable]
    public class GameState
    {
        public Vector2Int playerPosition;
        public List<Vector2Int> boxPositions;
        public int moveCount;
        public int pushCount;

        public GameState(Vector2Int playerPosition, IEnumerable<Vector2Int> boxPositions, int moveCount, int pushCount)
        {
            this.playerPosition = playerPosition;
            this.boxPositions = new List<Vector2Int>(boxPositions);
            this.moveCount = moveCount;
            this.pushCount = pushCount;
        }

        public GameState Clone()
        {
            return new GameState(playerPosition, boxPositions, moveCount, pushCount);
        }
    }
}
