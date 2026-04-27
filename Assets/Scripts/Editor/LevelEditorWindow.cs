#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GridShift.Data;
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
                MarkDirty();
            }
        }

        private void DrawToolSelector()
        {
            selectedTool = (PaintTool)GUILayout.Toolbar((int)selectedTool, new[] { "Wall", "Goal", "Box", "Player", "Erase" });
        }

        private void DrawGrid()
        {
            EditorGUILayout.Space(8f);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int y = currentLevel.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < currentLevel.width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    string label = GetCellLabel(position);
                    Color previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = GetCellColor(position);

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
                    currentLevel.walls.Clear();
                    currentLevel.goals.Clear();
                    currentLevel.boxes.Clear();
                    currentLevel.playerStart = new Vector2Int(-1, -1);
                    MarkDirty();
                }
            }

            EditorGUILayout.EndHorizontal();
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

            ClampDataToBounds();
            RemoveDuplicates();
            MarkDirty();
            AssetDatabase.SaveAssets();
            ValidateLevel();
        }

        private void Paint(Vector2Int position)
        {
            UnityEditor.Undo.RecordObject(currentLevel, "Paint GridShift Cell");

            switch (selectedTool)
            {
                case PaintTool.Wall:
                    AddUnique(currentLevel.walls, position);
                    currentLevel.goals.Remove(position);
                    currentLevel.boxes.Remove(position);
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
                    }
                    break;
                case PaintTool.PlayerStart:
                    if (!currentLevel.walls.Contains(position))
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
            MarkDirty();
        }

        private string GetCellLabel(Vector2Int position)
        {
            bool isPlayer = currentLevel.playerStart == position;
            bool hasWall = currentLevel.walls.Contains(position);
            bool hasGoal = currentLevel.goals.Contains(position);
            bool hasBox = currentLevel.boxes.Contains(position);

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

        private Color GetCellColor(Vector2Int position)
        {
            if (currentLevel.playerStart == position)
            {
                return new Color(0.4f, 0.8f, 1f);
            }

            if (currentLevel.walls.Contains(position))
            {
                return Color.gray;
            }

            if (currentLevel.boxes.Contains(position) && currentLevel.goals.Contains(position))
            {
                return new Color(0.5f, 1f, 0.5f);
            }

            if (currentLevel.boxes.Contains(position))
            {
                return new Color(1f, 0.75f, 0.3f);
            }

            if (currentLevel.goals.Contains(position))
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

            if (currentLevel.width < 1 || currentLevel.height < 1)
            {
                validationMessages.Add("Width and Height must be 1 or greater.");
            }

            if (!currentLevel.IsInside(currentLevel.playerStart))
            {
                validationMessages.Add("PlayerStart is not set or is outside the level bounds.");
            }

            if (currentLevel.boxes.Count != currentLevel.goals.Count)
            {
                validationMessages.Add("Box count and Goal count must be the same.");
            }

            HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(currentLevel.walls);
            foreach (Vector2Int box in currentLevel.boxes)
            {
                if (wallSet.Contains(box))
                {
                    validationMessages.Add($"Box overlaps Wall at {box}.");
                }
            }

            foreach (Vector2Int goal in currentLevel.goals)
            {
                if (wallSet.Contains(goal))
                {
                    validationMessages.Add($"Goal overlaps Wall at {goal}.");
                }
            }

            AddOutOfBoundsMessages("Wall", currentLevel.walls);
            AddOutOfBoundsMessages("Goal", currentLevel.goals);
            AddOutOfBoundsMessages("Box", currentLevel.boxes);

            if (validationMessages.Count == 0)
            {
                validationMessages.Add("OK: LevelData is valid.");
            }
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

        private void ClampDataToBounds()
        {
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
            currentLevel.walls = currentLevel.walls.Distinct().ToList();
            currentLevel.goals = currentLevel.goals.Distinct().ToList();
            currentLevel.boxes = currentLevel.boxes.Distinct().ToList();
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
