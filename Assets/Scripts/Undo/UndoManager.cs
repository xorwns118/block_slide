using System.Collections.Generic;
using GridShift.Data;

namespace GridShift.Undo
{
    public class UndoManager
    {
        private readonly Stack<GameState> stateStack = new Stack<GameState>();

        public int Count => stateStack.Count;

        public void Push(GameState state)
        {
            if (state == null)
            {
                return;
            }

            stateStack.Push(state.Clone());
        }

        public bool TryPop(out GameState state)
        {
            if (stateStack.Count == 0)
            {
                state = null;
                return false;
            }

            state = stateStack.Pop();
            return true;
        }

        public void Clear()
        {
            stateStack.Clear();
        }
    }
}
