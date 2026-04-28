using System.Collections.Generic;
using UnityEngine;

namespace GridShift.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "GridShift/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Min(1)] public int width = 8;
        [Min(1)] public int height = 8;
        public Vector2Int playerStart = new Vector2Int(-1, -1);
        public List<Vector2Int> walls = new List<Vector2Int>();
        public List<Vector2Int> goals = new List<Vector2Int>();
        public List<Vector2Int> boxes = new List<Vector2Int>();
        [Min(-1)] public int optimalMoveCount = -1;
        [Min(-1)] public int optimalPushCount = -1;

        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.y >= 0 && position.x < width && position.y < height;
        }
    }
}
