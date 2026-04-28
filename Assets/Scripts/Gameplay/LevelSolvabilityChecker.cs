using GridShift.Data;

namespace GridShift.Gameplay
{
    public static class LevelSolvabilityChecker
    {
        public static bool IsSolvable(LevelData levelData, out string reason)
        {
            LevelSolution solution = LevelOptimalSolver.FindOptimalSolution(levelData);
            reason = solution.Reason;
            return solution.IsSolved;
        }
    }
}
