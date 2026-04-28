using System.Collections.Generic;

namespace GridShift.Data
{
    public static class GameSession
    {
        public static LevelData SelectedLevel { get; private set; }
        public static List<LevelData> SelectedStageLevels { get; private set; } = new List<LevelData>();
        public static int StartingStageIndex { get; private set; }
        public static bool HasSelection { get; private set; }

        public static void PlaySingleLevel(LevelData level)
        {
            SelectedLevel = level;
            SelectedStageLevels.Clear();
            StartingStageIndex = 0;
            HasSelection = level != null;
        }

        public static void PlayStageSequence(IEnumerable<LevelData> levels, int startingIndex = 0)
        {
            SelectedLevel = null;
            SelectedStageLevels = levels != null ? new List<LevelData>(levels) : new List<LevelData>();
            StartingStageIndex = startingIndex;
            HasSelection = SelectedStageLevels.Count > 0;
        }

        public static void Clear()
        {
            SelectedLevel = null;
            SelectedStageLevels.Clear();
            StartingStageIndex = 0;
            HasSelection = false;
        }
    }
}
