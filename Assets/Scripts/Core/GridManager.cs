using System.Collections.Generic;
using GridShift.Data;
using GridShift.Gameplay;
using UnityEngine;

namespace GridShift.Core
{
    public enum TileType
    {
        Empty,
        Floor,
        Wall,
        Goal
    }

    public class GridManager
    {
        private readonly Dictionary<Vector2Int, TileType> tiles = new Dictionary<Vector2Int, TileType>();
        private readonly Dictionary<Vector2Int, BoxView> boxesByPosition = new Dictionary<Vector2Int, BoxView>();
        private readonly HashSet<Vector2Int> goals = new HashSet<Vector2Int>();

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize { get; }
        public Vector3 Origin { get; }

        public GridManager(float cellSize = 1f, Vector3 origin = default)
        {
            CellSize = Mathf.Max(0.01f, cellSize);
            Origin = origin;
        }

        public void Build(LevelData levelData)
        {
            tiles.Clear();
            boxesByPosition.Clear();
            goals.Clear();

            if (levelData == null)
            {
                Width = 0;
                Height = 0;
                return;
            }

            Width = Mathf.Max(1, levelData.width);
            Height = Mathf.Max(1, levelData.height);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    tiles[new Vector2Int(x, y)] = TileType.Floor;
                }
            }

            foreach (Vector2Int goal in levelData.goals)
            {
                if (!IsInside(goal))
                {
                    continue;
                }

                goals.Add(goal);
                tiles[goal] = TileType.Goal;
            }

            foreach (Vector2Int wall in levelData.walls)
            {
                if (!IsInside(wall))
                {
                    continue;
                }

                goals.Remove(wall);
                tiles[wall] = TileType.Wall;
            }
        }

        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.y >= 0 && position.x < Width && position.y < Height;
        }

        public TileType GetTile(Vector2Int position)
        {
            if (!IsInside(position))
            {
                return TileType.Empty;
            }

            return tiles.TryGetValue(position, out TileType tileType) ? tileType : TileType.Empty;
        }

        public bool IsWalkable(Vector2Int position)
        {
            TileType tileType = GetTile(position);
            return tileType == TileType.Floor || tileType == TileType.Goal;
        }

        public bool IsGoal(Vector2Int position)
        {
            return goals.Contains(position);
        }

        public HashSet<Vector2Int> GetGoalSet()
        {
            return new HashSet<Vector2Int>(goals);
        }

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return Origin + new Vector3(gridPosition.x * CellSize, gridPosition.y * CellSize, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - Origin;
            return new Vector2Int(Mathf.RoundToInt(local.x / CellSize), Mathf.RoundToInt(local.y / CellSize));
        }

        public void RegisterBox(BoxView boxView, Vector2Int position)
        {
            if (boxView == null || !IsInside(position))
            {
                return;
            }

            boxesByPosition[position] = boxView;
            boxView.SetGridPosition(position, GridToWorld(position));
        }

        public void ClearBoxes()
        {
            boxesByPosition.Clear();
        }

        public bool HasBox(Vector2Int position)
        {
            return boxesByPosition.ContainsKey(position);
        }

        public bool TryGetBox(Vector2Int position, out BoxView boxView)
        {
            return boxesByPosition.TryGetValue(position, out boxView);
        }

        public bool MoveBox(Vector2Int from, Vector2Int to)
        {
            if (!boxesByPosition.TryGetValue(from, out BoxView boxView) || boxesByPosition.ContainsKey(to))
            {
                return false;
            }

            boxesByPosition.Remove(from);
            boxesByPosition[to] = boxView;
            boxView.SetGridPosition(to, GridToWorld(to));
            return true;
        }

        public List<Vector2Int> GetBoxPositions()
        {
            return new List<Vector2Int>(boxesByPosition.Keys);
        }
    }
}
