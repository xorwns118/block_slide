using UnityEngine;

namespace GridShift.Gameplay
{
    public class BoxView : MonoBehaviour
    {
        public Vector2Int GridPosition { get; private set; }

        public void SetGridPosition(Vector2Int gridPosition, Vector3 worldPosition)
        {
            GridPosition = gridPosition;
            transform.position = worldPosition;
        }
    }
}
