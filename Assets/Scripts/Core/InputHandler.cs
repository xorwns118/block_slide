using UnityEngine;

namespace GridShift.Core
{
    public class InputHandler : MonoBehaviour
    {
        public bool TryGetMoveDirection(out Vector2Int direction)
        {
            direction = Vector2Int.zero;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                direction = Vector2Int.up;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                direction = Vector2Int.down;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                direction = Vector2Int.left;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                direction = Vector2Int.right;
            }

            return direction != Vector2Int.zero;
        }

        public bool IsUndoPressed()
        {
            return Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.Z);
        }

        public bool IsNextStagePressed()
        {
            return Input.GetKeyDown(KeyCode.N)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        public bool IsMainMenuPressed()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
    }
}
