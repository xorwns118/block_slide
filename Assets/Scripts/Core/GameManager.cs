using System.Collections.Generic;
using GridShift.Data;
using GridShift.Gameplay;
using GridShift.Undo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GridShift.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private LevelData levelData;
        [SerializeField] private List<LevelData> stageLevels = new List<LevelData>();
        [SerializeField] private int startingStageIndex;
        [SerializeField] private string mainSceneName = "MainScene";
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;
        [SerializeField] private bool centerLevelOnOrigin = true;
        [SerializeField] private bool autoScaleCellsToViewport = true;
        [SerializeField] private bool fitCameraToLevel = true;
        [SerializeField] private float cameraPadding = 1f;
        [SerializeField] private Camera targetCamera;

        [Header("Prefabs")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject goalPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject boxPrefab;

        [Header("UI")]
        [SerializeField] private Text moveCountText;
        [SerializeField] private Text pushCountText;
        [SerializeField] private Text clearText;
        [SerializeField] private Text stageText;
        [SerializeField] private Text starText;

        private GridManager gridManager;
        private LevelLoader levelLoader;
        private LoadedLevel loadedLevel;
        private InputHandler inputHandler;
        private MovementSystem movementSystem;
        private RuleChecker ruleChecker;
        private UndoManager undoManager;
        private int moveCount;
        private int pushCount;
        private int currentStageIndex;
        private bool isCleared;

        private void Awake()
        {
            inputHandler = GetComponent<InputHandler>();
            if (inputHandler == null)
            {
                inputHandler = gameObject.AddComponent<InputHandler>();
            }
        }

        private void Start()
        {
            ApplySessionSelection();
            currentStageIndex = Mathf.Max(0, startingStageIndex);
            if (!InitializeLevel())
            {
                Debug.LogError("Initial level load failed. Returning to the main menu.");
                ReturnToMainMenu();
            }
        }

        private void Update()
        {
            if (inputHandler == null)
            {
                return;
            }

            if (isCleared)
            {
                if (inputHandler.IsMainMenuPressed())
                {
                    ReturnToMainMenu();
                }
                else if (inputHandler.IsNextStagePressed())
                {
                    LoadNextStage();
                }
                else if (inputHandler.IsUndoPressed())
                {
                    UndoMove();
                }

                return;
            }

            if (movementSystem == null)
            {
                if (inputHandler.IsMainMenuPressed())
                {
                    ReturnToMainMenu();
                }

                return;
            }

            if (inputHandler.IsMainMenuPressed())
            {
                ReturnToMainMenu();
                return;
            }

            if (inputHandler.IsUndoPressed())
            {
                UndoMove();
                return;
            }

            if (inputHandler.TryGetMoveDirection(out Vector2Int direction))
            {
                TryMove(direction);
            }
        }

        public bool InitializeLevel()
        {
            LevelData selectedLevel = GetCurrentStageLevel();
            if (selectedLevel == null)
            {
                ClearRuntimeState();
                Debug.LogError("GameManager requires LevelData or a valid Stage Levels entry.");
                UpdateUi();
                return false;
            }

            if (!ValidateLevelDataForRuntime(selectedLevel))
            {
                ClearRuntimeState();
                UpdateUi();
                return false;
            }

            if (!ValidatePrefabReferencesForRuntime(selectedLevel))
            {
                ClearRuntimeState();
                UpdateUi();
                return false;
            }

            ClearRuntimeState();

            levelData = selectedLevel;
            float resolvedCellSize = ResolveCellSize(selectedLevel);
            Vector3 resolvedOrigin = ResolveGridOrigin(selectedLevel, resolvedCellSize);
            gridManager = new GridManager(resolvedCellSize, resolvedOrigin);
            levelLoader = new LevelLoader(gridManager, floorPrefab, wallPrefab, goalPrefab, playerPrefab, boxPrefab);
            loadedLevel = levelLoader.Load(selectedLevel, transform);

            if (loadedLevel == null || loadedLevel.Player == null)
            {
                ClearRuntimeState();
                Debug.LogError("Level failed to load. Check prefab and LevelData assignments.");
                UpdateUi();
                return false;
            }

            if (loadedLevel.Boxes.Count != selectedLevel.boxes.Count)
            {
                ClearRuntimeState();
                Debug.LogError("Level failed to load all boxes. Check Box prefab and LevelData assignments.");
                UpdateUi();
                return false;
            }

            movementSystem = new MovementSystem(gridManager, loadedLevel.Player, selectedLevel.playerStart);
            ruleChecker = new RuleChecker(gridManager.GetGoalSet());
            undoManager = new UndoManager();
            moveCount = 0;
            pushCount = 0;
            isCleared = false;

            FitCamera(selectedLevel, resolvedCellSize, resolvedOrigin);
            UpdateUi();
            CheckClear();
            return true;
        }

        public void LoadNextStage()
        {
            if (!HasStageSequence())
            {
                Debug.Log("No Stage Levels list is assigned. Staying on the current level.");
                return;
            }

            int nextStageIndex = currentStageIndex + 1;
            if (nextStageIndex >= stageLevels.Count)
            {
                Debug.Log("All GridShift stages are cleared.");
                return;
            }

            int previousStageIndex = currentStageIndex;
            currentStageIndex = nextStageIndex;
            if (!InitializeLevel())
            {
                currentStageIndex = previousStageIndex;
                Debug.LogError("Failed to load next stage. Returning to the main menu.");
                ReturnToMainMenu();
            }
        }

        public void ReturnToMainMenu()
        {
            ClearRuntimeState();
            GameSession.Clear();
            SceneManager.LoadScene(mainSceneName);
        }

        private void ApplySessionSelection()
        {
            if (!GameSession.HasSelection)
            {
                return;
            }

            if (GameSession.SelectedStageLevels.Count > 0)
            {
                stageLevels = new List<LevelData>(GameSession.SelectedStageLevels);
                startingStageIndex = GameSession.StartingStageIndex;
                levelData = stageLevels[Mathf.Clamp(startingStageIndex, 0, stageLevels.Count - 1)];
                return;
            }

            if (GameSession.SelectedLevel != null)
            {
                levelData = GameSession.SelectedLevel;
                stageLevels.Clear();
                startingStageIndex = 0;
            }
        }

        private LevelData GetCurrentStageLevel()
        {
            if (!HasStageSequence())
            {
                return levelData;
            }

            currentStageIndex = Mathf.Clamp(currentStageIndex, 0, stageLevels.Count - 1);
            return stageLevels[currentStageIndex];
        }

        private bool HasStageSequence()
        {
            return stageLevels != null && stageLevels.Count > 0;
        }

        private void ClearRuntimeState()
        {
            if (loadedLevel != null && loadedLevel.Root != null)
            {
                Destroy(loadedLevel.Root.gameObject);
            }

            gridManager = null;
            levelLoader = null;
            loadedLevel = null;
            movementSystem = null;
            ruleChecker = null;
            undoManager = null;
            moveCount = 0;
            pushCount = 0;
            isCleared = false;
        }

        private float ResolveCellSize(LevelData data)
        {
            float safeCellSize = Mathf.Max(0.01f, cellSize);
            if (!autoScaleCellsToViewport)
            {
                return safeCellSize;
            }

            Camera cameraToFit = GetTargetCamera();
            if (cameraToFit == null || !cameraToFit.orthographic || data == null)
            {
                return safeCellSize;
            }

            float safePadding = Mathf.Max(0f, cameraPadding);
            float verticalWorldSize = Mathf.Max(0.01f, cameraToFit.orthographicSize * 2f);
            float horizontalWorldSize = Mathf.Max(0.01f, verticalWorldSize * cameraToFit.aspect);
            float usableVerticalWorldSize = verticalWorldSize - safePadding * 2f;
            float usableHorizontalWorldSize = horizontalWorldSize - safePadding * 2f;

            if (usableVerticalWorldSize <= 0.01f || usableHorizontalWorldSize <= 0.01f)
            {
                Debug.LogWarning("Camera padding is too large for the current viewport. Keeping the configured cell size.");
                return safeCellSize;
            }

            float maxCellSizeByWidth = usableHorizontalWorldSize / Mathf.Max(1, data.width);
            float maxCellSizeByHeight = usableVerticalWorldSize / Mathf.Max(1, data.height);

            return Mathf.Min(safeCellSize, maxCellSizeByWidth, maxCellSizeByHeight);
        }

        private Vector3 ResolveGridOrigin(LevelData data, float resolvedCellSize)
        {
            if (!centerLevelOnOrigin || data == null)
            {
                return gridOrigin;
            }

            float xOffset = -((data.width - 1) * resolvedCellSize) * 0.5f;
            float yOffset = -((data.height - 1) * resolvedCellSize) * 0.5f;
            return gridOrigin + new Vector3(xOffset, yOffset, 0f);
        }

        private void FitCamera(LevelData data, float resolvedCellSize, Vector3 resolvedOrigin)
        {
            if (!fitCameraToLevel || data == null)
            {
                return;
            }

            Camera cameraToFit = GetTargetCamera();
            if (cameraToFit == null || !cameraToFit.orthographic)
            {
                return;
            }

            Vector3 cameraPosition = cameraToFit.transform.position;
            Vector3 levelCenter = GetLevelCenter(data, resolvedCellSize, resolvedOrigin);
            cameraToFit.transform.position = new Vector3(levelCenter.x, levelCenter.y, cameraPosition.z);

            if (autoScaleCellsToViewport)
            {
                return;
            }

            float safePadding = Mathf.Max(0f, cameraPadding);
            float levelWidth = Mathf.Max(1, data.width) * resolvedCellSize;
            float levelHeight = Mathf.Max(1, data.height) * resolvedCellSize;
            float verticalSize = (levelHeight + safePadding * 2f) * 0.5f;
            float horizontalSize = (levelWidth + safePadding * 2f) * 0.5f / Mathf.Max(0.01f, cameraToFit.aspect);
            cameraToFit.orthographicSize = Mathf.Max(verticalSize, horizontalSize, 0.01f);
        }

        private Vector3 GetLevelCenter(LevelData data, float resolvedCellSize, Vector3 resolvedOrigin)
        {
            float xOffset = ((Mathf.Max(1, data.width) - 1) * resolvedCellSize) * 0.5f;
            float yOffset = ((Mathf.Max(1, data.height) - 1) * resolvedCellSize) * 0.5f;
            return resolvedOrigin + new Vector3(xOffset, yOffset, 0f);
        }

        private Camera GetTargetCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            return Camera.main;
        }

        private void TryMove(Vector2Int direction)
        {
            GameState snapshot = CaptureState();
            MoveResult result = movementSystem.TryMove(direction);
            if (!result.Success)
            {
                return;
            }

            undoManager.Push(snapshot);
            moveCount++;
            if (result.PushedBox)
            {
                pushCount++;
            }

            UpdateUi();
            CheckClear();
        }

        private GameState CaptureState()
        {
            List<Vector2Int> boxPositions = gridManager != null ? GetSortedBoxPositions(gridManager.GetBoxPositions()) : new List<Vector2Int>();
            Vector2Int playerPosition = movementSystem != null ? movementSystem.PlayerPosition : Vector2Int.zero;
            return new GameState(playerPosition, boxPositions, moveCount, pushCount);
        }

        private void UndoMove()
        {
            if (undoManager == null || movementSystem == null || gridManager == null)
            {
                return;
            }

            if (!undoManager.TryPop(out GameState state))
            {
                return;
            }

            RestoreState(state);
            isCleared = false;
            UpdateUi();
            CheckClear();
        }

        private void RestoreState(GameState state)
        {
            if (state == null || loadedLevel == null)
            {
                return;
            }

            movementSystem.SetPlayerPosition(state.playerPosition);
            moveCount = state.moveCount;
            pushCount = state.pushCount;

            gridManager.ClearBoxes();
            List<Vector2Int> sortedPositions = GetSortedBoxPositions(state.boxPositions);
            int count = Mathf.Min(loadedLevel.Boxes.Count, sortedPositions.Count);
            for (int i = 0; i < count; i++)
            {
                gridManager.RegisterBox(loadedLevel.Boxes[i], sortedPositions[i]);
            }

            if (loadedLevel.Boxes.Count != sortedPositions.Count)
            {
                Debug.LogWarning("Undo restored with mismatched box count. Check level data integrity.");
            }
        }

        private bool ValidateLevelDataForRuntime(LevelData data)
        {
            if (data == null)
            {
                Debug.LogError("GameManager requires LevelData.");
                return false;
            }

            if (!data.IsInside(data.playerStart))
            {
                Debug.LogError("LevelData PlayerStart is not set or is outside the level bounds.");
                return false;
            }

            if (data.walls == null || data.goals == null || data.boxes == null)
            {
                Debug.LogError("LevelData Wall, Goal, and Box lists must not be null.");
                return false;
            }

            HashSet<Vector2Int> walls = new HashSet<Vector2Int>(data.walls);
            foreach (Vector2Int wall in data.walls)
            {
                if (!data.IsInside(wall))
                {
                    Debug.LogError($"LevelData Wall is outside the level bounds at {wall}.");
                    return false;
                }
            }

            if (walls.Contains(data.playerStart))
            {
                Debug.LogError($"LevelData PlayerStart overlaps Wall at {data.playerStart}.");
                return false;
            }

            HashSet<Vector2Int> boxes = new HashSet<Vector2Int>(data.boxes);
            HashSet<Vector2Int> goals = new HashSet<Vector2Int>(data.goals);
            if (boxes.Count != data.boxes.Count || goals.Count != data.goals.Count || walls.Count != data.walls.Count)
            {
                Debug.LogError("LevelData contains duplicate Wall, Goal, or Box positions.");
                return false;
            }

            if (data.boxes.Count != data.goals.Count)
            {
                Debug.LogError("LevelData Box count and Goal count must be the same.");
                return false;
            }

            if (boxes.Contains(data.playerStart))
            {
                Debug.LogError($"LevelData PlayerStart overlaps Box at {data.playerStart}.");
                return false;
            }

            foreach (Vector2Int box in boxes)
            {
                if (!data.IsInside(box))
                {
                    Debug.LogError($"LevelData Box is outside the level bounds at {box}.");
                    return false;
                }

                if (walls.Contains(box))
                {
                    Debug.LogError($"LevelData Box overlaps Wall at {box}.");
                    return false;
                }
            }

            foreach (Vector2Int goal in goals)
            {
                if (!data.IsInside(goal))
                {
                    Debug.LogError($"LevelData Goal is outside the level bounds at {goal}.");
                    return false;
                }

                if (walls.Contains(goal))
                {
                    Debug.LogError($"LevelData Goal overlaps Wall at {goal}.");
                    return false;
                }
            }

            if (!LevelSolvabilityChecker.IsSolvable(data, out string solveReason))
            {
                Debug.LogError($"LevelData is not solvable: {solveReason}");
                return false;
            }

            return true;
        }

        private bool ValidatePrefabReferencesForRuntime(LevelData data)
        {
            if (floorPrefab == null)
            {
                Debug.LogError("GameManager requires Floor prefab.");
                return false;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("GameManager requires Player prefab.");
                return false;
            }

            if (data.walls.Count > 0 && wallPrefab == null)
            {
                Debug.LogError("GameManager requires Wall prefab because the LevelData contains walls.");
                return false;
            }

            if (data.goals.Count > 0 && goalPrefab == null)
            {
                Debug.LogError("GameManager requires Goal prefab because the LevelData contains goals.");
                return false;
            }

            if (data.boxes.Count > 0 && boxPrefab == null)
            {
                Debug.LogError("GameManager requires Box prefab because the LevelData contains boxes.");
                return false;
            }

            return true;
        }

        private static List<Vector2Int> GetSortedBoxPositions(IEnumerable<Vector2Int> positions)
        {
            List<Vector2Int> sortedPositions = new List<Vector2Int>(positions);
            sortedPositions.Sort((a, b) =>
            {
                int yCompare = a.y.CompareTo(b.y);
                return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
            });

            return sortedPositions;
        }

        private void CheckClear()
        {
            if (ruleChecker == null || gridManager == null)
            {
                return;
            }

            if (ruleChecker.IsCleared(gridManager.GetBoxPositions()))
            {
                if (!isCleared)
                {
                    Debug.Log($"GridShift level cleared in {moveCount} moves.");
                    SaveClearProgress();
                }

                isCleared = true;
                UpdateUi();
            }
        }

        private void UpdateUi()
        {
            if (moveCountText != null)
            {
                moveCountText.text = $"Moves: {moveCount}";
            }

            if (pushCountText != null)
            {
                pushCountText.text = $"Pushes: {pushCount}";
            }

            if (clearText != null)
            {
                clearText.text = isCleared ? GetClearMessage() : string.Empty;
            }

            if (starText != null)
            {
                starText.text = isCleared ? GetStarMessage() : GetOptimalMessage();
            }

            if (stageText != null)
            {
                stageText.text = GetStageLabel();
            }
        }

        private string GetClearMessage()
        {
            if (!HasStageSequence())
            {
                return "CLEAR!";
            }

            return currentStageIndex < stageLevels.Count - 1 ? "CLEAR! Press N for next stage." : "ALL CLEAR!";
        }

        private string GetStarMessage()
        {
            if (levelData == null || levelData.optimalMoveCount < 0)
            {
                return "Stars: -";
            }

            int stars = StarRatingCalculator.CalculateStars(moveCount, levelData.optimalMoveCount);
            return $"Stars: {stars} / 3";
        }

        private void SaveClearProgress()
        {
            if (levelData == null)
            {
                return;
            }

            int stars = levelData.optimalMoveCount >= 0
                ? StarRatingCalculator.CalculateStars(moveCount, levelData.optimalMoveCount)
                : 1;
            LevelProgressStore.SaveResult(levelData, stars, moveCount, pushCount);
        }

        private string GetOptimalMessage()
        {
            if (levelData == null || levelData.optimalMoveCount < 0)
            {
                return "Best: Not calculated";
            }

            return $"Best: {levelData.optimalMoveCount} moves / {levelData.optimalPushCount} pushes";
        }

        private string GetStageLabel()
        {
            if (!HasStageSequence())
            {
                return levelData != null ? levelData.name : "No Level";
            }

            int displayIndex = Mathf.Clamp(currentStageIndex, 0, stageLevels.Count - 1) + 1;
            return $"Stage {displayIndex} / {stageLevels.Count}";
        }
    }
}
