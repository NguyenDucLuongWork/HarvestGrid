using System;
using UnityEngine;

namespace HarvestGrid.Leaderboard
{
    [Serializable]
    public class LeaderboardEntryData
    {
        public int Rank;                     // 1-based rank (1 = Top 1)
        public string PlayFabId;             // PlayFab Account ID
        public string DisplayName;           // Display Name or Fallback
        public float CompletionTime;         // Completion time in seconds
        public string AvatarUrl;             // Avatar URL if configured
        public bool IsCurrentPlayer;         // Whether this entry belongs to the local player
        public int RawStatValue;             // Raw PlayFab statistic value
    }

    [Serializable]
    public class LeaderboardSubmitResult
    {
        public bool Success;
        public bool IsNewBest;
        public float NewTime;
        public float PreviousBestTime;
        public string ErrorMessage;
    }

    public static class LeaderboardUtils
    {
        /// <summary>
        /// PlayFab statistic uses signed 32-bit int. We invert time in milliseconds
        /// from 1,000,000,000 (~11.5 days max level length) so that a smaller time
        /// produces a larger statistic value. Because PlayFab leaderboards sort DESCENDING,
        /// this guarantees that the fastest player is ranked #1.
        /// </summary>
        public const int MAX_TIME_MS = 1_000_000_000;

        /// <summary>
        /// Leaderboard ID naming rule: based on stable Level ID (e.g. Leaderboard_Level_1).
        /// </summary>
        public static string GetLeaderboardName(int levelId)
        {
            return $"Leaderboard_Level_{levelId}";
        }

        /// <summary>
        /// Converts level completion time in seconds to PlayFab descending statistic value.
        /// </summary>
        public static int TimeToStatistic(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int ms = Mathf.Clamp(Mathf.RoundToInt(seconds * 1000f), 0, MAX_TIME_MS);
            return MAX_TIME_MS - ms;
        }

        /// <summary>
        /// Converts PlayFab inverted statistic value back to actual completion time in seconds.
        /// </summary>
        public static float StatisticToTime(int statValue)
        {
            if (statValue <= 0 || statValue > MAX_TIME_MS)
                return 0f;

            int ms = MAX_TIME_MS - statValue;
            return ms / 1000f;
        }

        /// <summary>
        /// Formats seconds into human-readable string "MM:SS.s" (e.g. 02:15.3).
        /// Never displays inverted score or raw statistic to players.
        /// </summary>
        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
                return "--:--";

            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remSeconds = seconds % 60f;
            return $"{minutes:00}:{remSeconds:00.0}";
        }
    }
}

