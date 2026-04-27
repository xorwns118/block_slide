using UnityEngine;

namespace GridShift.Core
{
    public class InputHandler : MonoBehaviour
    {
        public bool TryGetMoveDirection(out Vector2Int direction)
        {
            direction = Vector2Int.zero;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                direction = Vector2Int.up;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                direction = Vector2Int.down;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                direction = Vector2Int.left;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                direction = Vector2Int.right;
            }

            return direction != Vector2Int.zero;
        }

        public bool IsUndoPressed()
        {
            return Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.Z);
        }
    }
}
