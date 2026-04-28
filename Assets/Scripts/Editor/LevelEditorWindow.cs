#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridShift.Data;
using GridShift.Gameplay;
using UnityEditor;
using UnityEngine;

namespace GridShift.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private enum PaintTool
        {
            Wall,
            Goal,
            Box,
            PlayerStart,
            Erase
        }

        private LevelData currentLevel;
        private PaintTool selectedTool = PaintTool.Wall;
        private Vector2 scrollPosition;
        private readonly List<string> validationMessages = new List<string>();

        [MenuItem("Tools/GridShift/Level Editor")]
        public static void Open()
        {
            GetWindow<LevelEditorWindow>("GridShift Level Editor");
        }

        private void OnGUI()
        {
            DrawAssetControls();

            if (currentLevel == null)
            {
                EditorGUILayout.HelpBox("Create or select a LevelData asset to start editing.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            DrawLevelSettings();
            DrawOptimalSolution();
            DrawToolSelector();
            DrawGrid();
            DrawActions();
            DrawValidationMessages();
        }

        private void DrawAssetControls()
        {
            EditorGUILayout.LabelField("Level Asset", EditorStyles.boldLabel);
            currentLevel = (LevelData)EditorGUILayout.ObjectField("Current Level", currentLevel, typeof(LevelData), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create LevelData"))
            {
                CreateLevelData();
            }

            if (GUILayout.Button("Save"))
            {
                SaveLevel();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelSettings()
        {
            EditorGUI.BeginChangeCheck();
            int width = EditorGUILayout.IntField("Width", currentLevel.width);
            int height = EditorGUILayout.IntField("Height", currentLevel.height);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(currentLevel, "Resize GridShift Level");
                currentLevel.width = Mathf.Max(1, width);
                currentLevel.height = Mathf.Max(1, height);
                ClampDataToBounds();
                InvalidateOptimalSolution();
                MarkDirty();
                ValidateLevel();
            }
        }

        private void DrawToolSelector()
        {
            selectedTool = (PaintTool)GUILayout.Toolbar((int)selectedTool, new[] { "Wall", "Goal", "Box", "Player", "Erase" });
        }

        private void DrawOptimalSolution()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Optimal Solution", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Minimum Moves", currentLevel.optimalMoveCount >= 0 ? currentLevel.optimalMoveCount.ToString() : "Not calculated");
            EditorGUILayout.LabelField("Minimum Pushes", currentLevel.optimalPushCount >= 0 ? currentLevel.optimalPushCount.ToString() : "Not calculated");

            if (GUILayout.Button("Calculate Optimal Solution"))
            {
                CalculateOptimalSolution();
            }
        }

        private void DrawGrid()
        {
            EditorGUILayout.Space(8f);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            List<Vector2Int> walls = currentLevel.walls ?? new List<Vector2Int>();
            List<Vector2Int> goals = currentLevel.goals ?? new List<Vector2Int>();
            List<Vector2Int> boxes = currentLevel.boxes ?? new List<Vector2Int>();

            for (int y = currentLevel.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < currentLevel.width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    string label = GetCellLabel(position, walls, goals, boxes);
                    Color previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = GetCellColor(position, walls, goals, boxes);

                    if (GUILayout.Button(label, GUILayout.Width(36f), GUILayout.Height(30f)))
                    {
                        Paint(position);
                    }

                    GUI.backgroundColor = previousColor;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Validate"))
            {
                ValidateLevel();
            }

            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear Level", "Remove all walls, goals, boxes, and reset player start?", "Clear", "Cancel"))
                {
                    UnityEditor.Undo.RecordObject(currentLevel, "Clear GridShift Level");
                    EnsureListsExist();
                    currentLevel.walls.Clear();
                    currentLevel.goals.Clear();
                    currentLevel.boxes.Clear();
                    currentLevel.playerStart = new Vector2Int(-1, -1);
                    InvalidateOptimalSolution();
                    MarkDirty();
                    ValidateLevel();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CalculateOptimalSolution()
        {
            if (currentLevel == null)
            {
                return;
            }

            ValidateLevel();
            if (HasValidationWarnings())
            {
                validationMessages.Insert(0, "Resolve validation issues before calculating the optimal solution.");
                return;
            }

            LevelSolution solution = LevelOptimalSolver.FindOptimalSolution(currentLevel);
            UnityEditor.Undo.RecordObject(currentLevel, "Calculate GridShift Optimal Solution");

            if (solution.IsSolved)
            {
                currentLevel.optimalMoveCount = solution.MoveCount;
                currentLevel.optimalPushCount = solution.PushCount;
                validationMessages.Clear();
                validationMessages.Add($"OK: Optimal solution saved. Moves: {solution.MoveCount}, Pushes: {solution.PushCount}, States: {solution.ExploredStateCount}.");
            }
            else
            {
                currentLevel.optimalMoveCount = -1;
                currentLevel.optimalPushCount = -1;
                validationMessages.Clear();
                validationMessages.Add($"Optimal solution failed: {solution.Reason}");
            }

            MarkDirty();
            AssetDatabase.SaveAssets();
        }

        private void DrawValidationMessages()
        {
            if (validationMessages.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            foreach (string message in validationMessages)
            {
                MessageType type = message.StartsWith("OK") ? MessageType.Info : MessageType.Warning;
                EditorGUILayout.HelpBox(message, type);
            }
        }

        private bool HasValidationWarnings()
        {
            return validationMessages.Any(message => !message.StartsWith("OK"));
        }

        private void CreateLevelData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create GridShift LevelData",
                "LevelData",
                "asset",
                "Choose where to save the new LevelData asset.",
                "Assets/ScriptableObjects");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            LevelData asset = CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            currentLevel = asset;
            Selection.activeObject = currentLevel;
        }

        private void SaveLevel()
        {
            if (currentLevel == null)
            {
                return;
            }

            ValidateLevel();
            string beforeNormalizeSignature = BuildLevelSignature();
            ClampDataToBounds();
            RemoveDuplicates();
            if (beforeNormalizeSignature != BuildLevelSignature())
            {
                InvalidateOptimalSolution();
            }

            MarkDirty();
            AssetDatabase.SaveAssets();
            ValidateLevel();

            if (HasValidationWarnings())
            {
                validationMessages.Insert(0, "Saved and normalized. Remaining issues are listed below.");
            }
            else
            {
                validationMessages.Clear();
                validationMessages.Add("OK: LevelData saved, normalized, and valid.");
            }
        }

        private void Paint(Vector2Int position)
        {
            EnsureListsExist();
            UnityEditor.Undo.RecordObject(currentLevel, "Paint GridShift Cell");

            switch (selectedTool)
            {
                case PaintTool.Wall:
                    AddUnique(currentLevel.walls, position);
                    currentLevel.goals.Remove(position);
                    currentLevel.boxes.Remove(position);
                    if (currentLevel.playerStart == position)
                    {
                        currentLevel.playerStart = new Vector2Int(-1, -1);
                    }
                    break;
                case PaintTool.Goal:
                    if (!currentLevel.walls.Contains(position))
                    {
                        AddUnique(currentLevel.goals, position);
                    }
                    break;
                case PaintTool.Box:
                    if (!currentLevel.walls.Contains(position))
                    {
                        AddUnique(currentLevel.boxes, position);
                        if (currentLevel.playerStart == position)
                        {
                            currentLevel.playerStart = new Vector2Int(-1, -1);
                        }
                    }
                    break;
                case PaintTool.PlayerStart:
                    if (!currentLevel.walls.Contains(position) && !currentLevel.boxes.Contains(position))
                    {
                        currentLevel.playerStart = position;
                    }
                    break;
                case PaintTool.Erase:
                    currentLevel.walls.Remove(position);
                    currentLevel.goals.Remove(position);
                    currentLevel.boxes.Remove(position);
                    if (currentLevel.playerStart == position)
                    {
                        currentLevel.playerStart = new Vector2Int(-1, -1);
                    }
                    break;
            }

            RemoveDuplicates();
            InvalidateOptimalSolution();
            MarkDirty();
        }

        private string GetCellLabel(Vector2Int position, List<Vector2Int> walls, List<Vector2Int> goals, List<Vector2Int> boxes)
        {
            bool isPlayer = currentLevel.playerStart == position;
            bool hasWall = walls.Contains(position);
            bool hasGoal = goals.Contains(position);
            bool hasBox = boxes.Contains(position);

            if (isPlayer)
            {
                return "P";
            }

            if (hasWall)
            {
                return "W";
            }

            if (hasBox && hasGoal)
            {
                return "B/G";
            }

            if (hasBox)
            {
                return "B";
            }

            if (hasGoal)
            {
                return "G";
            }

            return ".";
        }

        private Color GetCellColor(Vector2Int position, List<Vector2Int> walls, List<Vector2Int> goals, List<Vector2Int> boxes)
        {
            if (currentLevel.playerStart == position)
            {
                return new Color(0.4f, 0.8f, 1f);
            }

            if (walls.Contains(position))
            {
                return Color.gray;
            }

            if (boxes.Contains(position) && goals.Contains(position))
            {
                return new Color(0.5f, 1f, 0.5f);
            }

            if (boxes.Contains(position))
            {
                return new Color(1f, 0.75f, 0.3f);
            }

            if (goals.Contains(position))
            {
                return new Color(0.6f, 1f, 0.6f);
            }

            return Color.white;
        }

        private void ValidateLevel()
        {
            validationMessages.Clear();

            if (currentLevel == null)
            {
                validationMessages.Add("LevelData is not selected.");
                return;
            }

            List<Vector2Int> walls = GetSafeList("Wall", currentLevel.walls);
            List<Vector2Int> goals = GetSafeList("Goal", currentLevel.goals);
            List<Vector2Int> boxes = GetSafeList("Box", currentLevel.boxes);

            if (currentLevel.width < 1 || currentLevel.height < 1)
            {
                validationMessages.Add("Width and Height must be 1 or greater.");
            }

            if (!currentLevel.IsInside(currentLevel.playerStart))
            {
                validationMessages.Add("PlayerStart is not set or is outside the level bounds.");
            }

            if (walls.Contains(currentLevel.playerStart))
            {
                validationMessages.Add($"PlayerStart overlaps Wall at {currentLevel.playerStart}.");
            }

            if (boxes.Contains(currentLevel.playerStart))
            {
                validationMessages.Add($"PlayerStart overlaps Box at {currentLevel.playerStart}.");
            }

            if (boxes.Count != goals.Count)
            {
                validationMessages.Add("Box count and Goal count must be the same.");
            }

            HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(walls);
            foreach (Vector2Int box in boxes)
            {
                if (wallSet.Contains(box))
                {
                    validationMessages.Add($"Box overlaps Wall at {box}.");
                }
            }

            foreach (Vector2Int goal in goals)
            {
                if (wallSet.Contains(goal))
                {
                    validationMessages.Add($"Goal overlaps Wall at {goal}.");
                }
            }

            AddOutOfBoundsMessages("Wall", walls);
            AddOutOfBoundsMessages("Goal", goals);
            AddOutOfBoundsMessages("Box", boxes);
            AddDuplicateMessages("Wall", walls);
            AddDuplicateMessages("Goal", goals);
            AddDuplicateMessages("Box", boxes);

            if (validationMessages.Count == 0 && !LevelSolvabilityChecker.IsSolvable(currentLevel, out string solveReason))
            {
                validationMessages.Add($"Level is not solvable: {solveReason}");
            }

            if (validationMessages.Count == 0)
            {
                validationMessages.Add("OK: LevelData is valid.");
            }
        }

        private List<Vector2Int> GetSafeList(string label, List<Vector2Int> positions)
        {
            if (positions != null)
            {
                return positions;
            }

            validationMessages.Add($"{label} list is null.");
            return new List<Vector2Int>();
        }

        private void AddOutOfBoundsMessages(string label, IEnumerable<Vector2Int> positions)
        {
            foreach (Vector2Int position in positions)
            {
                if (!currentLevel.IsInside(position))
                {
                    validationMessages.Add($"{label} is out of bounds at {position}.");
                }
            }
        }

        private void AddDuplicateMessages(string label, IEnumerable<Vector2Int> positions)
        {
            HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
            HashSet<Vector2Int> reported = new HashSet<Vector2Int>();

            foreach (Vector2Int position in positions)
            {
                if (seen.Add(position))
                {
                    continue;
                }

                if (reported.Add(position))
                {
                    validationMessages.Add($"{label} has duplicate position at {position}.");
                }
            }
        }

        private void ClampDataToBounds()
        {
            EnsureListsExist();
            currentLevel.walls.RemoveAll(position => !currentLevel.IsInside(position));
            currentLevel.goals.RemoveAll(position => !currentLevel.IsInside(position));
            currentLevel.boxes.RemoveAll(position => !currentLevel.IsInside(position));

            if (!currentLevel.IsInside(currentLevel.playerStart))
            {
                currentLevel.playerStart = new Vector2Int(-1, -1);
            }
        }

        private void RemoveDuplicates()
        {
            EnsureListsExist();
            currentLevel.walls = currentLevel.walls.Distinct().ToList();
            currentLevel.goals = currentLevel.goals.Distinct().ToList();
            currentLevel.boxes = currentLevel.boxes.Distinct().ToList();
        }

        private void InvalidateOptimalSolution()
        {
            currentLevel.optimalMoveCount = -1;
            currentLevel.optimalPushCount = -1;
        }

        private string BuildLevelSignature()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(currentLevel.width);
            builder.Append('x');
            builder.Append(currentLevel.height);
            builder.Append('|');
            builder.Append(currentLevel.playerStart.x);
            builder.Append(',');
            builder.Append(currentLevel.playerStart.y);
            builder.Append('|');
            AppendPositions(builder, currentLevel.walls);
            builder.Append('|');
            AppendPositions(builder, currentLevel.goals);
            builder.Append('|');
            AppendPositions(builder, currentLevel.boxes);
            return builder.ToString();
        }

        private static void AppendPositions(StringBuilder builder, IEnumerable<Vector2Int> positions)
        {
            if (positions == null)
            {
                builder.Append("null");
                return;
            }

            List<Vector2Int> sorted = positions.ToList();
            sorted.Sort((a, b) =>
            {
                int yCompare = a.y.CompareTo(b.y);
                return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
            });

            foreach (Vector2Int position in sorted)
            {
                builder.Append(position.x);
                builder.Append(',');
                builder.Append(position.y);
                builder.Append(';');
            }
        }

        private void EnsureListsExist()
        {
            if (currentLevel.walls == null)
            {
                currentLevel.walls = new List<Vector2Int>();
            }

            if (currentLevel.goals == null)
            {
                currentLevel.goals = new List<Vector2Int>();
            }

            if (currentLevel.boxes == null)
            {
                currentLevel.boxes = new List<Vector2Int>();
            }
        }

        private static void AddUnique(List<Vector2Int> list, Vector2Int position)
        {
            if (!list.Contains(position))
            {
                list.Add(position);
            }
        }

        private void MarkDirty()
        {
            EditorUtility.SetDirty(currentLevel);
            Repaint();
        }
    }
}
#endif
