using System.Collections.Generic;
using GridShift.Core;
using GridShift.Gameplay;
using UnityEngine;

namespace GridShift.Data
{
    public class LoadedLevel
    {
        public PlayerView Player { get; }
        public List<BoxView> Boxes { get; }
        public Transform Root { get; }

        public LoadedLevel(PlayerView player, List<BoxView> boxes, Transform root)
        {
            Player = player;
            Boxes = boxes;
            Root = root;
        }
    }

    public class LevelLoader
    {
        private readonly GridManager gridManager;
        private readonly GameObject floorPrefab;
        private readonly GameObject wallPrefab;
        private readonly GameObject goalPrefab;
        private readonly GameObject playerPrefab;
        private readonly GameObject boxPrefab;

        public LevelLoader(
            GridManager gridManager,
            GameObject floorPrefab,
            GameObject wallPrefab,
            GameObject goalPrefab,
            GameObject playerPrefab,
            GameObject boxPrefab)
        {
            this.gridManager = gridManager;
            this.floorPrefab = floorPrefab;
            this.wallPrefab = wallPrefab;
            this.goalPrefab = goalPrefab;
            this.playerPrefab = playerPrefab;
            this.boxPrefab = boxPrefab;
        }

        public LoadedLevel Load(LevelData levelData, Transform parent = null)
        {
            if (levelData == null || gridManager == null)
            {
                Debug.LogError("LevelLoader requires a LevelData and GridManager.");
                return null;
            }

            gridManager.Build(levelData);

            GameObject rootObject = new GameObject($"Level_{levelData.name}");
            Transform root = rootObject.transform;
            if (parent != null)
            {
                root.SetParent(parent);
            }

            HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(levelData.walls);
            HashSet<Vector2Int> goalSet = new HashSet<Vector2Int>(levelData.goals);

            for (int y = 0; y < levelData.height; y++)
            {
                for (int x = 0; x < levelData.width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    Spawn(floorPrefab, position, root, "Floor");

                    if (goalSet.Contains(position))
                    {
                        Spawn(goalPrefab, position, root, "Goal");
                    }

                    if (wallSet.Contains(position))
                    {
                        Spawn(wallPrefab, position, root, "Wall");
                    }
                }
            }

            PlayerView player = SpawnPlayer(levelData.playerStart, root);
            List<BoxView> boxes = SpawnBoxes(levelData.boxes, root);

            return new LoadedLevel(player, boxes, root);
        }

        private void Spawn(GameObject prefab, Vector2Int position, Transform parent, string fallbackName)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = Object.Instantiate(prefab, gridManager.GridToWorld(position), Quaternion.identity, parent);
            instance.name = $"{fallbackName}_{position.x}_{position.y}";
        }

        private PlayerView SpawnPlayer(Vector2Int position, Transform parent)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab is not assigned.");
                return null;
            }

            GameObject instance = Object.Instantiate(playerPrefab, gridManager.GridToWorld(position), Quaternion.identity, parent);
            instance.name = "Player";

            PlayerView playerView = instance.GetComponent<PlayerView>();
            if (playerView == null)
            {
                playerView = instance.AddComponent<PlayerView>();
            }

            playerView.SetGridPosition(position, gridManager.GridToWorld(position));
            return playerView;
        }

        private List<BoxView> SpawnBoxes(IEnumerable<Vector2Int> positions, Transform parent)
        {
            List<BoxView> boxes = new List<BoxView>();
            if (boxPrefab == null || positions == null)
            {
                if (boxPrefab == null)
                {
                    Debug.LogError("Box prefab is not assigned.");
                }

                return boxes;
            }

            foreach (Vector2Int position in positions)
            {
                if (!gridManager.IsWalkable(position) || gridManager.HasBox(position))
                {
                    Debug.LogWarning($"Skipped invalid box position: {position}");
                    continue;
                }

                GameObject instance = Object.Instantiate(boxPrefab, gridManager.GridToWorld(position), Quaternion.identity, parent);
                instance.name = $"Box_{position.x}_{position.y}";

                BoxView boxView = instance.GetComponent<BoxView>();
                if (boxView == null)
                {
                    boxView = instance.AddComponent<BoxView>();
                }

                gridManager.RegisterBox(boxView, position);
                boxes.Add(boxView);
            }

            return boxes;
        }
    }
}
