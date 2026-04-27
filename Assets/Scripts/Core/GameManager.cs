using System.Collections.Generic;
using GridShift.Data;
using GridShift.Gameplay;
using GridShift.Undo;
using UnityEngine;
using UnityEngine.UI;

namespace GridShift.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private LevelData levelData;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;

        [Header("Prefabs")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject goalPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject boxPrefab;

        [Header("UI")]
        [SerializeField] private Text moveCountText;
        [SerializeField] private Text clearText;

        private GridManager gridManager;
        private LevelLoader levelLoader;
        private LoadedLevel loadedLevel;
        private InputHandler inputHandler;
        private MovementSystem movementSystem;
        private RuleChecker ruleChecker;
        private UndoManager undoManager;
        private int moveCount;
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
            InitializeLevel();
        }

        private void Update()
        {
            if (movementSystem == null || inputHandler == null || isCleared)
            {
                if (inputHandler != null && inputHandler.IsUndoPressed())
                {
                    UndoMove();
                }

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

        public void InitializeLevel()
        {
            if (levelData == null)
            {
                Debug.LogError("GameManager requires LevelData.");
                UpdateUi();
                return;
            }

            if (!levelData.IsInside(levelData.playerStart))
            {
                Debug.LogError("LevelData PlayerStart is not set or is outside the level bounds.");
                UpdateUi();
                return;
            }

            if (loadedLevel != null && loadedLevel.Root != null)
            {
                Destroy(loadedLevel.Root.gameObject);
            }

            gridManager = new GridManager(cellSize, gridOrigin);
            levelLoader = new LevelLoader(gridManager, floorPrefab, wallPrefab, goalPrefab, playerPrefab, boxPrefab);
            loadedLevel = levelLoader.Load(levelData, transform);

            if (loadedLevel == null || loadedLevel.Player == null)
            {
                Debug.LogError("Level failed to load. Check prefab and LevelData assignments.");
                return;
            }

            movementSystem = new MovementSystem(gridManager, loadedLevel.Player, levelData.playerStart);
            ruleChecker = new RuleChecker(gridManager.GetGoalSet());
            undoManager = new UndoManager();
            moveCount = 0;
            isCleared = false;

            UpdateUi();
            CheckClear();
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
            UpdateUi();
            CheckClear();
        }

        private GameState CaptureState()
        {
            List<Vector2Int> boxPositions = gridManager != null ? gridManager.GetBoxPositions() : new List<Vector2Int>();
            Vector2Int playerPosition = movementSystem != null ? movementSystem.PlayerPosition : Vector2Int.zero;
            return new GameState(playerPosition, boxPositions, moveCount);
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

            gridManager.ClearBoxes();
            int count = Mathf.Min(loadedLevel.Boxes.Count, state.boxPositions.Count);
            for (int i = 0; i < count; i++)
            {
                gridManager.RegisterBox(loadedLevel.Boxes[i], state.boxPositions[i]);
            }

            if (loadedLevel.Boxes.Count != state.boxPositions.Count)
            {
                Debug.LogWarning("Undo restored with mismatched box count. Check level data integrity.");
            }
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

            if (clearText != null)
            {
                clearText.text = isCleared ? "CLEAR!" : string.Empty;
            }
        }
    }
}
