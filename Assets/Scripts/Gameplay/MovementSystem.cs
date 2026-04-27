using GridShift.Core;
using UnityEngine;

namespace GridShift.Gameplay
{
    public readonly struct MoveResult
    {
        public bool Success { get; }
        public bool PushedBox { get; }

        public MoveResult(bool success, bool pushedBox)
        {
            Success = success;
            PushedBox = pushedBox;
        }
    }

    public class MovementSystem
    {
        private readonly GridManager gridManager;
        private readonly PlayerView playerView;

        public Vector2Int PlayerPosition { get; private set; }

        public MovementSystem(GridManager gridManager, PlayerView playerView, Vector2Int playerStart)
        {
            this.gridManager = gridManager;
            this.playerView = playerView;
            PlayerPosition = playerStart;

            if (this.playerView != null && this.gridManager != null)
            {
                this.playerView.SetGridPosition(playerStart, this.gridManager.GridToWorld(playerStart));
            }
        }

        public MoveResult TryMove(Vector2Int direction)
        {
            if (gridManager == null || playerView == null || direction == Vector2Int.zero)
            {
                return new MoveResult(false, false);
            }

            Vector2Int target = PlayerPosition + direction;
            if (!gridManager.IsWalkable(target))
            {
                return new MoveResult(false, false);
            }

            if (gridManager.HasBox(target))
            {
                Vector2Int boxTarget = target + direction;
                if (!gridManager.IsWalkable(boxTarget) || gridManager.HasBox(boxTarget))
                {
                    return new MoveResult(false, false);
                }

                if (!gridManager.MoveBox(target, boxTarget))
                {
                    return new MoveResult(false, false);
                }

                SetPlayerPosition(target);
                return new MoveResult(true, true);
            }

            SetPlayerPosition(target);
            return new MoveResult(true, false);
        }

        public void SetPlayerPosition(Vector2Int position)
        {
            PlayerPosition = position;

            if (playerView != null && gridManager != null)
            {
                playerView.SetGridPosition(position, gridManager.GridToWorld(position));
            }
        }
    }
}
