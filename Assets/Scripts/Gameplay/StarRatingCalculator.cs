using UnityEngine;

namespace GridShift.Gameplay
{
    public static class StarRatingCalculator
    {
        public static int CalculateStars(int actualMoveCount, int optimalMoveCount)
        {
            if (actualMoveCount < 0 || optimalMoveCount < 0)
            {
                return 1;
            }

            if (actualMoveCount <= optimalMoveCount)
            {
                return 3;
            }

            int twoStarLimit = optimalMoveCount + Mathf.Max(2, Mathf.CeilToInt(optimalMoveCount * 0.5f));
            return actualMoveCount <= twoStarLimit ? 2 : 1;
        }
    }
}
