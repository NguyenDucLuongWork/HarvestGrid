using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestGrid.Leaderboard
{
    public class LevelCompletionLeaderboardHandler : MonoBehaviour
    {
        [Header("UI Status Indicators")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button viewLeaderboardButton;
        [SerializeField] private LeaderboardUI leaderboardUI;

        [Header("Status Colors")]
        [SerializeField] private Color submittingColor = new Color(0.95f, 0.75f, 0.2f); // Golden amber
        [SerializeField] private Color successColor = new Color(0.2f, 0.75f, 0.35f);    // Soft farm green
        [SerializeField] private Color failedColor = new Color(0.9f, 0.35f, 0.3f);      // Red / Coral

        private int cachedLevelId = -1;
        private float cachedCompletionTime = 0f;
        private bool cachedIsNewBest = true;
        private float cachedPreviousBest = 0f;
        private bool isSubmitting = false;
        private bool hasSubmittedSuccessfully = false;

        private void Awake()
        {
            EnsureUI();
            HookListeners();
        }

        private void OnDestroy()
        {
            UnhookListeners();
        }

        private void HookListeners()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
                retryButton.gameObject.SetActive(false);
            }

            if (viewLeaderboardButton != null)
            {
                viewLeaderboardButton.onClick.AddListener(OnViewLeaderboardClicked);
            }
        }

        private void UnhookListeners()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryClicked);

            if (viewLeaderboardButton != null)
                viewLeaderboardButton.onClick.RemoveListener(OnViewLeaderboardClicked);
        }

        public void EnsureUI()
        {
            // Status Text
            if (statusText == null)
            {
                var stGo = transform.Find("LeaderboardStatusText")?.gameObject;
                if (stGo == null)
                {
                    stGo = new GameObject("LeaderboardStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    stGo.transform.SetParent(transform, false);
                    var stRect = stGo.GetComponent<RectTransform>();
                    stRect.anchorMin = new Vector2(0.5f, 0.5f);
                    stRect.anchorMax = new Vector2(0.5f, 0.5f);
                    stRect.pivot = new Vector2(0.5f, 0.5f);
                    stRect.anchoredPosition = new Vector2(0f, 170f);
                    stRect.sizeDelta = new Vector2(900f, 60f);
                }
                statusText = stGo.GetComponent<TextMeshProUGUI>();
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.fontSize = 26f;
                statusText.text = "";
            }

            // Retry Button
            if (retryButton == null)
            {
                var rbGo = transform.Find("LeaderboardRetryBtn")?.gameObject;
                if (rbGo == null)
                {
                    rbGo = new GameObject("LeaderboardRetryBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    rbGo.transform.SetParent(transform, false);
                    var rbRect = rbGo.GetComponent<RectTransform>();
                    rbRect.anchorMin = new Vector2(0.5f, 0.5f);
                    rbRect.anchorMax = new Vector2(0.5f, 0.5f);
                    rbRect.pivot = new Vector2(0.5f, 0.5f);
                    rbRect.anchoredPosition = new Vector2(0f, 105f);
                    rbRect.sizeDelta = new Vector2(220f, 55f);

                    rbGo.GetComponent<Image>().color = new Color(0.85f, 0.45f, 0.25f);

                    var tGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    tGo.transform.SetParent(rbGo.transform, false);
                    var txt = tGo.GetComponent<TextMeshProUGUI>();
                    txt.text = "Thử lại";
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontSize = 24f;
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = Color.white;
                }
                retryButton = rbGo.GetComponent<Button>();
                retryButton.gameObject.SetActive(false);
            }

            // View Leaderboard Button
            if (viewLeaderboardButton == null)
            {
                var vlbGo = transform.Find("ViewLeaderboardBtn")?.gameObject;
                if (vlbGo == null)
                {
                    vlbGo = new GameObject("ViewLeaderboardBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    vlbGo.transform.SetParent(transform, false);
                    var vRect = vlbGo.GetComponent<RectTransform>();
                    vRect.anchorMin = new Vector2(0.5f, 0.5f);
                    vRect.anchorMax = new Vector2(0.5f, 0.5f);
                    vRect.pivot = new Vector2(0.5f, 0.5f);
                    vRect.anchoredPosition = new Vector2(0f, -170f);
                    vRect.sizeDelta = new Vector2(380f, 75f);

                    vlbGo.GetComponent<Image>().color = new Color(0.35f, 0.65f, 0.35f);

                    var vtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    vtGo.transform.SetParent(vlbGo.transform, false);
                    var vtxt = vtGo.GetComponent<TextMeshProUGUI>();
                    vtxt.text = "BẢNG XẾP HẠNG";
                    vtxt.alignment = TextAlignmentOptions.Center;
                    vtxt.fontSize = 26f;
                    vtxt.fontStyle = FontStyles.Bold;
                    vtxt.color = Color.white;
                }
                viewLeaderboardButton = vlbGo.GetComponent<Button>();
            }
        }

        /// <summary>
        /// Initiates the leaderboard submission when level is won.
        /// </summary>
        public void HandleLevelCompleted(int levelId, float completionTime, bool isNewBest = true, float previousBestTime = 0f)
        {
            EnsureUI();

            cachedLevelId = levelId;
            cachedCompletionTime = completionTime;
            cachedIsNewBest = isNewBest;
            cachedPreviousBest = previousBestTime;
            hasSubmittedSuccessfully = false;

            Submit();
        }

        public void Submit()
        {
            if (isSubmitting || hasSubmittedSuccessfully)
                return;

            if (!cachedIsNewBest)
            {
                SetStatus($"Hoàn thành! Kỷ lục tốt nhất ({LeaderboardUtils.FormatTime(cachedPreviousBest)}) được giữ nguyên.", successColor, false);
                return;
            }

            var mgr = LeaderboardManager.GetOrCreate();

            if (!mgr.IsAuthenticated)
            {
                SetStatus("Chưa đăng nhập PlayFab. Không thể lưu bảng xếp hạng.", failedColor, true);
                return;
            }

            isSubmitting = true;
            SetStatus("Đang gửi kết quả lên Bảng xếp hạng...", submittingColor, false);

            mgr.SubmitLevelScore(
                cachedLevelId,
                cachedCompletionTime,
                result =>
                {
                    isSubmitting = false;

                    if (result.Success)
                    {
                        hasSubmittedSuccessfully = true;
                        if (result.IsNewBest)
                        {
                            SetStatus("Đã gửi thành tích mới thành công!", successColor, false);
                        }
                        else
                        {
                            SetStatus($"Hoàn thành! Kỷ lục tốt nhất ({LeaderboardUtils.FormatTime(result.PreviousBestTime)}) được giữ nguyên.", successColor, false);
                        }
                    }
                    else
                    {
                        string msg = string.IsNullOrEmpty(result.ErrorMessage)
                            ? "Gửi kết quả thất bại."
                            : $"Gửi kết quả thất bại: {result.ErrorMessage}";
                        SetStatus(msg, failedColor, true);
                    }
                },
                cachedIsNewBest,
                cachedPreviousBest
            );
        }

        private void OnRetryClicked()
        {
            Submit();
        }

        private void OnViewLeaderboardClicked()
        {
            if (leaderboardUI == null)
            {
                leaderboardUI = FindAnyObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
            }

            if (leaderboardUI == null)
            {
                // Instantiate LeaderboardUI under the GameplayScene Canvas if missing
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var lbGo = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(LeaderboardUI));
                    lbGo.transform.SetParent(canvas.transform, false);
                    leaderboardUI = lbGo.GetComponent<LeaderboardUI>();
                }
            }

            if (leaderboardUI != null)
            {
                leaderboardUI.Open(cachedLevelId);
            }
        }

        private void SetStatus(string message, Color color, bool showRetry)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(showRetry);
            }
        }
    }
}
