using UnityEngine;

namespace GridShift.Data
{
    public static class LevelProgressStore
    {
        public static int GetBestStars(LevelData level)
        {
            return level != null ? PlayerPrefs.GetInt(BuildKey(level, "Stars"), 0) : 0;
        }

        public static int GetBestMoves(LevelData level)
        {
            return level != null ? PlayerPrefs.GetInt(BuildKey(level, "Moves"), -1) : -1;
        }

        public static int GetBestPushes(LevelData level)
        {
            return level != null ? PlayerPrefs.GetInt(BuildKey(level, "Pushes"), -1) : -1;
        }

        public static void SaveResult(LevelData level, int stars, int moves, int pushes)
        {
            if (level == null)
            {
                return;
            }

            int bestStars = GetBestStars(level);
            int bestMoves = GetBestMoves(level);
            int bestPushes = GetBestPushes(level);

            if (stars > bestStars)
            {
                PlayerPrefs.SetInt(BuildKey(level, "Stars"), stars);
            }

            if (bestMoves < 0 || moves < bestMoves)
            {
                PlayerPrefs.SetInt(BuildKey(level, "Moves"), moves);
            }

            if (bestPushes < 0 || pushes < bestPushes)
            {
                PlayerPrefs.SetInt(BuildKey(level, "Pushes"), pushes);
            }

            PlayerPrefs.Save();
        }

        private static string BuildKey(LevelData level, string suffix)
        {
            return $"GridShift.{level.name}.{suffix}";
        }
    }
}
