using System;
using System.Collections.Generic;
using LgTyLib.Core;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using HarvestGrid.UI;

namespace HarvestGrid.Leaderboard
{
    public class LeaderboardManager : BaseSingleton<LeaderboardManager>
    {
        private int currentRequestId = 0;
        private bool isDisplayNameSynced = false;

        public static LeaderboardManager GetOrCreate()
        {
            if (HasInstance) return Instance;
            var go = new GameObject("LeaderboardManager");
            return go.AddComponent<LeaderboardManager>();
        }

        protected override void Awake()
        {
            base.Awake();
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Checks if the local player is currently authenticated with PlayFab.
        /// </summary>
        public bool IsAuthenticated =>
            PlayFabClientAPI.IsClientLoggedIn() &&
            AuthSession.IsAuthenticated &&
            !string.IsNullOrEmpty(AuthSession.UserId);

        /// <summary>
        /// Current player's PlayFab Id.
        /// </summary>
        public string CurrentPlayerId => AuthSession.UserId;

        /// <summary>
        /// Current player's Username.
        /// </summary>
        public string CurrentPlayerUsername =>
            string.IsNullOrEmpty(AuthSession.Username) ? "Player" : AuthSession.Username;

        /// <summary>
        /// Submits completion time for a level to PlayFab.
        /// Only submits if authenticated, completion time is valid, and better than prior best.
        /// </summary>
        public void SubmitLevelScore(
            int levelId,
            float completionTime,
            Action<LeaderboardSubmitResult> onComplete = null,
            bool isNewBest = true,
            float previousBestTime = 0f)
        {
            if (levelId < 0)
            {
                onComplete?.Invoke(new LeaderboardSubmitResult
                {
                    Success = false,
                    ErrorMessage = "ID màn chơi không hợp lệ."
                });
                return;
            }

            if (completionTime <= 0f || float.IsNaN(completionTime) || float.IsInfinity(completionTime))
            {
                onComplete?.Invoke(new LeaderboardSubmitResult
                {
                    Success = false,
                    ErrorMessage = "Thời gian hoàn thành không hợp lệ."
                });
                return;
            }

            if (!IsAuthenticated)
            {
                onComplete?.Invoke(new LeaderboardSubmitResult
                {
                    Success = false,
                    ErrorMessage = "Bạn cần đăng nhập PlayFab để lưu thành tích bảng xếp hạng."
                });
                return;
            }

            // Sync DisplayName to PlayFab if needed so the leaderboard displays the user's name
            EnsureDisplayNameSynced();

            // Client-side best record check (Requirement 3.4)
            if (!isNewBest)
            {
                Debug.Log($"[LeaderboardManager] Current time ({completionTime:F2}s) is not faster than best ({previousBestTime:F2}s). Keeping existing record.");
                onComplete?.Invoke(new LeaderboardSubmitResult
                {
                    Success = true,
                    IsNewBest = false,
                    NewTime = completionTime,
                    PreviousBestTime = previousBestTime,
                    ErrorMessage = null
                });
                return;
            }

            string statName = LeaderboardUtils.GetLeaderboardName(levelId);
            int statValue = LeaderboardUtils.TimeToStatistic(completionTime);

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = statName,
                        Value = statValue
                    }
                }
            };

            Debug.Log($"[LeaderboardManager] Submitting score for Level {levelId} (Stat: {statName}, Value: {statValue}, Time: {completionTime:F2}s)...");

            PlayFabClientAPI.UpdatePlayerStatistics(
                request,
                result =>
                {
                    Debug.Log($"[LeaderboardManager] Score submitted successfully for Level {levelId}!");
                    onComplete?.Invoke(new LeaderboardSubmitResult
                    {
                        Success = true,
                        IsNewBest = true,
                        NewTime = completionTime,
                        ErrorMessage = null
                    });
                },
                error =>
                {
                    Debug.LogError($"[LeaderboardManager] Failed to submit score for Level {levelId}: {error.GenerateErrorReport()}");
                    string errorMsg = ParsePlayFabError(error);
                    onComplete?.Invoke(new LeaderboardSubmitResult
                    {
                        Success = false,
                        IsNewBest = false,
                        NewTime = completionTime,
                        ErrorMessage = errorMsg
                    });
                }
            );
        }

        /// <summary>
        /// Retrieves the Top 50 players for a specific level.
        /// Handles rapid level switching by discarding stale responses.
        /// </summary>
        public void GetTop50(
            int levelId,
            Action<List<LeaderboardEntryData>> onSuccess,
            Action<string> onError)
        {
            if (levelId < 0)
            {
                onError?.Invoke("ID màn chơi không hợp lệ.");
                return;
            }

            if (!IsAuthenticated)
            {
                onError?.Invoke("Vui lòng đăng nhập tài khoản PlayFab để xem bảng xếp hạng.");
                return;
            }

            int requestId = ++currentRequestId;
            string statName = LeaderboardUtils.GetLeaderboardName(levelId);

            var request = new GetLeaderboardRequest
            {
                StatisticName = statName,
                StartPosition = 0,
                MaxResultsCount = 50,
                ProfileConstraints = new PlayerProfileViewConstraints
                {
                    ShowDisplayName = true,
                    ShowAvatarUrl = true
                }
            };

            PlayFabClientAPI.GetLeaderboard(
                request,
                result =>
                {
                    // Discard stale callback if user navigated to another level
                    if (requestId != currentRequestId)
                    {
                        Debug.Log($"[LeaderboardManager] Discarding stale response for {statName}.");
                        return;
                    }

                    var entries = new List<LeaderboardEntryData>();
                    if (result.Leaderboard != null)
                    {
                        foreach (var item in result.Leaderboard)
                        {
                            entries.Add(CreateEntryData(item));
                        }
                    }

                    onSuccess?.Invoke(entries);
                },
                error =>
                {
                    if (requestId != currentRequestId) return;

                    // If statistic has not been created yet or has no entries, treat as empty list
                    if (error.Error == PlayFabErrorCode.StatisticNotFound)
                    {
                        Debug.Log($"[LeaderboardManager] Statistic {statName} not yet initialized. Returning empty list.");
                        onSuccess?.Invoke(new List<LeaderboardEntryData>());
                        return;
                    }

                    Debug.LogError($"[LeaderboardManager] GetTop50 failed for {statName}: {error.GenerateErrorReport()}");
                    onError?.Invoke(ParsePlayFabError(error));
                }
            );
        }

        /// <summary>
        /// Retrieves current player's record for a specific level, even if outside Top 50.
        /// </summary>
        public void GetPlayerRecord(
            int levelId,
            Action<LeaderboardEntryData> onSuccess,
            Action<string> onError)
        {
            if (levelId < 0)
            {
                onError?.Invoke("ID màn chơi không hợp lệ.");
                return;
            }

            if (!IsAuthenticated)
            {
                onError?.Invoke("Vui lòng đăng nhập tài khoản PlayFab để xem thành tích.");
                return;
            }

            int requestId = currentRequestId;
            string statName = LeaderboardUtils.GetLeaderboardName(levelId);

            var request = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = statName,
                MaxResultsCount = 1,
                ProfileConstraints = new PlayerProfileViewConstraints
                {
                    ShowDisplayName = true,
                    ShowAvatarUrl = true
                }
            };

            PlayFabClientAPI.GetLeaderboardAroundPlayer(
                request,
                result =>
                {
                    if (requestId != currentRequestId) return;

                    if (result.Leaderboard != null && result.Leaderboard.Count > 0)
                    {
                        var playerEntry = result.Leaderboard.Find(e => e.PlayFabId == CurrentPlayerId)
                                          ?? result.Leaderboard[0];
                        onSuccess?.Invoke(CreateEntryData(playerEntry));
                    }
                    else
                    {
                        onSuccess?.Invoke(null);
                    }
                },
                error =>
                {
                    if (requestId != currentRequestId) return;

                    if (error.Error == PlayFabErrorCode.StatisticNotFound)
                    {
                        onSuccess?.Invoke(null);
                        return;
                    }

                    Debug.LogError($"[LeaderboardManager] GetPlayerRecord failed for {statName}: {error.GenerateErrorReport()}");
                    onError?.Invoke(ParsePlayFabError(error));
                }
            );
        }

        /// <summary>
        /// Updates PlayFab Title DisplayName with current username so player's name appears on leaderboard.
        /// </summary>
        public void EnsureDisplayNameSynced()
        {
            if (isDisplayNameSynced || !IsAuthenticated)
                return;

            string nameToSet = AuthSession.Username;
            if (string.IsNullOrWhiteSpace(nameToSet))
                return;

            PlayFabClientAPI.UpdateUserTitleDisplayName(
                new UpdateUserTitleDisplayNameRequest
                {
                    DisplayName = nameToSet
                },
                result =>
                {
                    isDisplayNameSynced = true;
                    Debug.Log($"[LeaderboardManager] PlayFab DisplayName synchronized: {result.DisplayName}");
                },
                error =>
                {
                    // Name might already be set or duplicate constraint; non-fatal
                    isDisplayNameSynced = true;
                    Debug.LogWarning($"[LeaderboardManager] DisplayName sync notice: {error.ErrorMessage}");
                }
            );
        }

        private LeaderboardEntryData CreateEntryData(PlayerLeaderboardEntry item)
        {
            bool isMe = (item.PlayFabId == CurrentPlayerId);

            string name = item.DisplayName;
            if (string.IsNullOrEmpty(name))
            {
                if (isMe)
                {
                    name = CurrentPlayerUsername;
                }
                else
                {
                    int len = Mathf.Min(6, item.PlayFabId.Length);
                    name = "Farmer_" + item.PlayFabId.Substring(0, len);
                }
            }

            return new LeaderboardEntryData
            {
                Rank = item.Position + 1,
                PlayFabId = item.PlayFabId,
                DisplayName = name,
                CompletionTime = LeaderboardUtils.StatisticToTime(item.StatValue),
                AvatarUrl = item.Profile?.AvatarUrl,
                IsCurrentPlayer = isMe,
                RawStatValue = item.StatValue
            };
        }

        private string ParsePlayFabError(PlayFabError error)
        {
            if (error == null) return "Lỗi không xác định.";

            if (error.Error == PlayFabErrorCode.ConnectionError ||
                error.Error == PlayFabErrorCode.ServiceUnavailable)
            {
                return "Mất kết nối Internet hoặc máy chủ PlayFab tạm thời không phản hồi. Vui lòng thử lại.";
            }

            if (error.Error == PlayFabErrorCode.NotAuthenticated)
            {
                return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
            }

            return error.ErrorMessage ?? "Đã xảy ra lỗi khi kết nối PlayFab.";
        }
    }
}
