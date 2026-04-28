#if UNITY_EDITOR
using System.Collections.Generic;
using GridShift.Data;
using GridShift.Gameplay;
using UnityEditor;
using UnityEngine;

namespace GridShift.Editor
{
    public static class SampleLevelGenerator
    {
        private const string OutputFolder = "Assets/ScriptableObjects";

        [MenuItem("Tools/GridShift/Generate Sample Levels")]
        public static void GenerateSampleLevels()
        {
            EnsureOutputFolder();

            CreateOrReplaceLevel(
                "Level_001_Intro",
                6,
                5,
                new Vector2Int(2, 2),
                BuildBorderWalls(6, 5),
                new List<Vector2Int> { new Vector2Int(4, 2) },
                new List<Vector2Int> { new Vector2Int(3, 2) });

            CreateOrReplaceLevel(
                "Level_002_CornerLesson",
                7,
                6,
                new Vector2Int(2, 2),
                BuildBorderWalls(7, 6),
                new List<Vector2Int>
                {
                    new Vector2Int(5, 2),
                    new Vector2Int(5, 3)
                },
                new List<Vector2Int>
                {
                    new Vector2Int(3, 2),
                    new Vector2Int(3, 3)
                });

            CreateOrReplaceLevel(
                "Level_003_TwoBoxHall",
                8,
                6,
                new Vector2Int(1, 3),
                Combine(
                    BuildBorderWalls(8, 6),
                    new List<Vector2Int>
                    {
                        new Vector2Int(2, 1),
                        new Vector2Int(2, 4),
                        new Vector2Int(5, 1),
                        new Vector2Int(5, 4)
                    }),
                new List<Vector2Int>
                {
                    new Vector2Int(6, 2),
                    new Vector2Int(6, 3)
                },
                new List<Vector2Int>
                {
                    new Vector2Int(3, 2),
                    new Vector2Int(4, 3)
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GridShift sample LevelData assets generated.");
        }

        private static void CreateOrReplaceLevel(
            string assetName,
            int width,
            int height,
            Vector2Int playerStart,
            List<Vector2Int> walls,
            List<Vector2Int> goals,
            List<Vector2Int> boxes)
        {
            string path = $"{OutputFolder}/{assetName}.asset";
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);

            if (levelData == null)
            {
                levelData = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(levelData, path);
            }

            UnityEditor.Undo.RecordObject(levelData, $"Generate {assetName}");
            levelData.width = width;
            levelData.height = height;
            levelData.playerStart = playerStart;
            levelData.walls = new List<Vector2Int>(walls);
            levelData.goals = new List<Vector2Int>(goals);
            levelData.boxes = new List<Vector2Int>(boxes);

            LevelSolution solution = LevelOptimalSolver.FindOptimalSolution(levelData);
            if (solution.IsSolved)
            {
                levelData.optimalMoveCount = solution.MoveCount;
                levelData.optimalPushCount = solution.PushCount;
            }
            else
            {
                levelData.optimalMoveCount = -1;
                levelData.optimalPushCount = -1;
                Debug.LogError($"{assetName} was generated but is not solvable: {solution.Reason}");
            }

            EditorUtility.SetDirty(levelData);
        }

        private static void EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
        }

        private static List<Vector2Int> BuildBorderWalls(int width, int height)
        {
            List<Vector2Int> walls = new List<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                walls.Add(new Vector2Int(x, 0));
                walls.Add(new Vector2Int(x, height - 1));
            }

            for (int y = 1; y < height - 1; y++)
            {
                walls.Add(new Vector2Int(0, y));
                walls.Add(new Vector2Int(width - 1, y));
            }

            return walls;
        }

        private static List<Vector2Int> Combine(List<Vector2Int> first, List<Vector2Int> second)
        {
            HashSet<Vector2Int> combined = new HashSet<Vector2Int>(first);

            foreach (Vector2Int position in second)
            {
                combined.Add(position);
            }

            return new List<Vector2Int>(combined);
        }
    }
}
#endif
