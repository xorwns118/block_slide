using System.Collections.Generic;
using System.Text;
using GridShift.Data;
using UnityEngine;

namespace GridShift.Gameplay
{
    public static class LevelOptimalSolver
    {
        private const int DefaultMaxIterations = 100000;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private sealed class SearchNode
        {
            public Vector2Int PlayerPosition;
            public List<Vector2Int> BoxPositions;
            public int MoveCost;
            public int PushCost;
            public SearchNode Previous;
            public Vector2Int DirectionFromPrevious;

            public string Key => BuildStateKey(PlayerPosition, BoxPositions);
        }

        private readonly struct Cost
        {
            public readonly int Moves;
            public readonly int Pushes;

            public Cost(int moves, int pushes)
            {
                Moves = moves;
                Pushes = pushes;
            }

            public bool IsBetterThan(Cost other)
            {
                return Moves < other.Moves || (Moves == other.Moves && Pushes < other.Pushes);
            }
        }

        public static LevelSolution FindOptimalSolution(LevelData levelData, int maxIterations = DefaultMaxIterations)
        {
            if (!TryPrepare(levelData, out HashSet<Vector2Int> walls, out HashSet<Vector2Int> goals, out List<Vector2Int> startBoxes, out string reason))
            {
                return LevelSolution.Failed(reason, 0);
            }

            SearchNode startNode = new SearchNode
            {
                PlayerPosition = levelData.playerStart,
                BoxPositions = startBoxes,
                MoveCost = 0,
                PushCost = 0
            };

            MinHeap openSet = new MinHeap();
            Dictionary<string, Cost> bestCosts = new Dictionary<string, Cost>();
            openSet.Enqueue(startNode);
            bestCosts[startNode.Key] = new Cost(0, 0);

            int iterations = 0;
            while (openSet.Count > 0)
            {
                if (++iterations > maxIterations)
                {
                    return LevelSolution.Failed($"Solver stopped after {maxIterations} states. The level may be too complex or unsolvable.", iterations);
                }

                SearchNode current = openSet.Dequeue();
                if (bestCosts.TryGetValue(current.Key, out Cost bestCost)
                    && (current.MoveCost > bestCost.Moves || current.PushCost > bestCost.Pushes))
                {
                    continue;
                }

                if (AreAllBoxesOnGoals(current.BoxPositions, goals))
                {
                    return new LevelSolution(
                        true,
                        current.MoveCost,
                        current.PushCost,
                        iterations,
                        $"Solved in {current.MoveCost} moves and {current.PushCost} pushes after exploring {iterations} states.",
                        BuildPath(current));
                }

                HashSet<Vector2Int> currentBoxes = new HashSet<Vector2Int>(current.BoxPositions);
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int playerTarget = current.PlayerPosition + direction;
                    if (!IsWalkable(levelData, walls, playerTarget))
                    {
                        continue;
                    }

                    bool pushedBox = false;
                    List<Vector2Int> nextBoxes = current.BoxPositions;
                    if (currentBoxes.Contains(playerTarget))
                    {
                        Vector2Int boxTarget = playerTarget + direction;
                        if (!IsWalkable(levelData, walls, boxTarget) || currentBoxes.Contains(boxTarget))
                        {
                            continue;
                        }

                        pushedBox = true;
                        nextBoxes = MoveBox(current.BoxPositions, playerTarget, boxTarget);
                    }

                    SearchNode nextNode = new SearchNode
                    {
                        PlayerPosition = playerTarget,
                        BoxPositions = nextBoxes,
                        MoveCost = current.MoveCost + 1,
                        PushCost = current.PushCost + (pushedBox ? 1 : 0),
                        Previous = current,
                        DirectionFromPrevious = direction
                    };

                    string key = nextNode.Key;
                    Cost nextCost = new Cost(nextNode.MoveCost, nextNode.PushCost);
                    if (bestCosts.TryGetValue(key, out Cost knownCost) && !nextCost.IsBetterThan(knownCost))
                    {
                        continue;
                    }

                    bestCosts[key] = nextCost;
                    openSet.Enqueue(nextNode);
                }
            }

            return LevelSolution.Failed("No solution found.", iterations);
        }

        private static bool TryPrepare(
            LevelData levelData,
            out HashSet<Vector2Int> walls,
            out HashSet<Vector2Int> goals,
            out List<Vector2Int> startBoxes,
            out string reason)
        {
            walls = new HashSet<Vector2Int>();
            goals = new HashSet<Vector2Int>();
            startBoxes = new List<Vector2Int>();
            reason = string.Empty;

            if (levelData == null)
            {
                reason = "LevelData is null.";
                return false;
            }

            if (levelData.walls == null || levelData.goals == null || levelData.boxes == null)
            {
                reason = "LevelData lists must not be null.";
                return false;
            }

            if (!levelData.IsInside(levelData.playerStart))
            {
                reason = "PlayerStart is not set or is outside the level bounds.";
                return false;
            }

            if (levelData.boxes.Count != levelData.goals.Count)
            {
                reason = "Box count and Goal count must be the same.";
                return false;
            }

            walls = new HashSet<Vector2Int>(levelData.walls);
            goals = new HashSet<Vector2Int>(levelData.goals);
            startBoxes = SortPositions(levelData.boxes);

            if (goals.Count == 0)
            {
                reason = "At least one Goal is required.";
                return false;
            }

            return true;
        }

        private static bool IsWalkable(LevelData levelData, HashSet<Vector2Int> walls, Vector2Int position)
        {
            return levelData.IsInside(position) && !walls.Contains(position);
        }

        private static bool AreAllBoxesOnGoals(IEnumerable<Vector2Int> boxes, HashSet<Vector2Int> goals)
        {
            foreach (Vector2Int box in boxes)
            {
                if (!goals.Contains(box))
                {
                    return false;
                }
            }

            return goals.Count > 0;
        }

        private static List<Vector2Int> MoveBox(List<Vector2Int> boxPositions, Vector2Int from, Vector2Int to)
        {
            List<Vector2Int> movedBoxes = new List<Vector2Int>(boxPositions);
            for (int i = 0; i < movedBoxes.Count; i++)
            {
                if (movedBoxes[i] == from)
                {
                    movedBoxes[i] = to;
                    break;
                }
            }

            return SortPositions(movedBoxes);
        }

        private static List<Vector2Int> SortPositions(IEnumerable<Vector2Int> positions)
        {
            List<Vector2Int> sorted = new List<Vector2Int>(positions);
            sorted.Sort((a, b) =>
            {
                int yCompare = a.y.CompareTo(b.y);
                return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
            });

            return sorted;
        }

        private static List<Vector2Int> BuildPath(SearchNode node)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            SearchNode current = node;
            while (current != null && current.Previous != null)
            {
                path.Add(current.DirectionFromPrevious);
                current = current.Previous;
            }

            path.Reverse();
            return path;
        }

        private static string BuildStateKey(Vector2Int playerPosition, IEnumerable<Vector2Int> boxPositions)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(playerPosition.x);
            builder.Append(',');
            builder.Append(playerPosition.y);
            builder.Append('|');

            foreach (Vector2Int box in boxPositions)
            {
                builder.Append(box.x);
                builder.Append(',');
                builder.Append(box.y);
                builder.Append(';');
            }

            return builder.ToString();
        }

        private sealed class MinHeap
        {
            private readonly List<SearchNode> nodes = new List<SearchNode>();

            public int Count => nodes.Count;

            public void Enqueue(SearchNode node)
            {
                nodes.Add(node);
                BubbleUp(nodes.Count - 1);
            }

            public SearchNode Dequeue()
            {
                SearchNode root = nodes[0];
                SearchNode last = nodes[nodes.Count - 1];
                nodes.RemoveAt(nodes.Count - 1);

                if (nodes.Count > 0)
                {
                    nodes[0] = last;
                    BubbleDown(0);
                }

                return root;
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (Compare(nodes[index], nodes[parent]) >= 0)
                    {
                        return;
                    }

                    Swap(index, parent);
                    index = parent;
                }
            }

            private void BubbleDown(int index)
            {
                while (true)
                {
                    int left = index * 2 + 1;
                    int right = left + 1;
                    int smallest = index;

                    if (left < nodes.Count && Compare(nodes[left], nodes[smallest]) < 0)
                    {
                        smallest = left;
                    }

                    if (right < nodes.Count && Compare(nodes[right], nodes[smallest]) < 0)
                    {
                        smallest = right;
                    }

                    if (smallest == index)
                    {
                        return;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int a, int b)
            {
                SearchNode temp = nodes[a];
                nodes[a] = nodes[b];
                nodes[b] = temp;
            }

            private static int Compare(SearchNode a, SearchNode b)
            {
                int moveCompare = a.MoveCost.CompareTo(b.MoveCost);
                return moveCompare != 0 ? moveCompare : a.PushCost.CompareTo(b.PushCost);
            }
        }
    }
}
