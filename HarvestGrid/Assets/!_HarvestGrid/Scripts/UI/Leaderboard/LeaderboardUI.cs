using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LgTyLib.Modules.Audio;

namespace HarvestGrid.Leaderboard
{
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("Window & Overlay")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundOverlayButton;

        [Header("Header & Level Selector")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private Button prevLevelButton;
        [SerializeField] private Button nextLevelButton;

        [Header("Player Best Record Banner")]
        [SerializeField] private GameObject playerBannerContainer;
        [SerializeField] private Image playerAvatarImage;
        [SerializeField] private TextMeshProUGUI playerUsernameText;
        [SerializeField] private TextMeshProUGUI playerRankText;
        [SerializeField] private TextMeshProUGUI playerBestTimeText;

        [Header("Scroll View & Rows")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform rowsContainer;
        [SerializeField] private LeaderboardRowUI rowPrefab;
        [SerializeField] private Sprite defaultAvatarSprite;

        [Header("Status Views")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject emptyStateView;
        [SerializeField] private GameObject errorView;
        [SerializeField] private TextMeshProUGUI errorMessageText;
        [SerializeField] private Button retryButton;

        // Runtime state
        private int currentLevelId = 1;
        private int minLevelId = 0;
        private int maxLevelId = 4;
        private readonly List<LeaderboardRowUI> pooledRows = new();
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            HookButtonListeners();
        }

        private void OnDestroy()
        {
            UnhookButtonListeners();
        }

        private void HookButtonListeners()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (backgroundOverlayButton != null)
                backgroundOverlayButton.onClick.AddListener(Close);

            if (prevLevelButton != null)
                prevLevelButton.onClick.AddListener(OnPrevLevelClicked);

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        private void UnhookButtonListeners()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (backgroundOverlayButton != null)
                backgroundOverlayButton.onClick.RemoveListener(Close);

            if (prevLevelButton != null)
                prevLevelButton.onClick.RemoveListener(OnPrevLevelClicked);

            if (nextLevelButton != null)
                nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
        }

        public void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            // Determine level bounds from LevelsManager if available
            if (LevelsManager.HasInstance && LevelsManager.Instance.Levels != null && LevelsManager.Instance.Levels.Length > 0)
            {
                var levels = LevelsManager.Instance.Levels;
                minLevelId = levels[0].levelID;
                maxLevelId = levels[levels.Length - 1].levelID;
                foreach (var lvl in levels)
                {
                    if (lvl != null)
                    {
                        if (lvl.levelID < minLevelId) minLevelId = lvl.levelID;
                        if (lvl.levelID > maxLevelId) maxLevelId = lvl.levelID;
                    }
                }
            }

            BuildUIIfMissing();
        }

        /// <summary>
        /// Opens the leaderboard panel for the specified level ID (or defaults to current/first level).
        /// </summary>
        public void Open(int? levelId = null)
        {
            Initialize();

            if (levelId.HasValue)
            {
                currentLevelId = levelId.Value;
            }
            else if (LevelsManager.HasInstance && LevelsManager.Instance.Levels != null && LevelsManager.Instance.Levels.Length > 0)
            {
                currentLevelId = LevelsManager.Instance.Levels[0].levelID;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            var mgr = LeaderboardManager.GetOrCreate();
            mgr.EnsureDisplayNameSynced();

            LoadLeaderboard(currentLevelId);
        }

        /// <summary>
        /// Closes the leaderboard panel.
        /// </summary>
        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnPrevLevelClicked()
        {
            int nextId = currentLevelId - 1;
            if (nextId >= minLevelId)
            {
                LoadLeaderboard(nextId);
            }
        }

        private void OnNextLevelClicked()
        {
            int nextId = currentLevelId + 1;
            if (nextId <= maxLevelId)
            {
                LoadLeaderboard(nextId);
            }
        }

        private void OnRetryClicked()
        {
            LoadLeaderboard(currentLevelId);
        }

        /// <summary>
        /// Loads both the Top 50 leaderboard and the player's personal record.
        /// </summary>
        public void LoadLeaderboard(int levelId)
        {
            currentLevelId = levelId;
            UpdateLevelHeader();

            ShowLoading();

            var mgr = LeaderboardManager.GetOrCreate();

            if (!mgr.IsAuthenticated)
            {
                ShowError("Vui lòng đăng nhập PlayFab để xem bảng xếp hạng.");
                ResetPlayerBanner(null);
                return;
            }

            // 1. Fetch Top 50
            mgr.GetTop50(
                levelId,
                entries =>
                {
                    OnTop50Loaded(entries);
                },
                error =>
                {
                    ShowError(error);
                }
            );

            // 2. Fetch Player's own record (in case outside Top 50)
            mgr.GetPlayerRecord(
                levelId,
                playerEntry =>
                {
                    ResetPlayerBanner(playerEntry);
                },
                error =>
                {
                    // Non-fatal, fallback to local finishedLevels or none
                    Debug.LogWarning($"[LeaderboardUI] Could not fetch personal record: {error}");
                    ResetPlayerBanner(null);
                }
            );
        }

        private void UpdateLevelHeader()
        {
            if (levelNameText != null)
            {
                levelNameText.text = $"Level {currentLevelId}";
            }

            if (prevLevelButton != null)
            {
                prevLevelButton.interactable = (currentLevelId > minLevelId);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.interactable = (currentLevelId < maxLevelId);
            }
        }

        private void OnTop50Loaded(List<LeaderboardEntryData> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                ShowEmpty();
                return;
            }

            ShowContent();

            // Ensure rowPrefab exists
            EnsureRowPrefab();

            // Populate rows using pooling
            int count = entries.Count;
            for (int i = 0; i < count; i++)
            {
                LeaderboardRowUI row;
                if (i < pooledRows.Count)
                {
                    row = pooledRows[i];
                }
                else
                {
                    row = Instantiate(rowPrefab, rowsContainer);
                    pooledRows.Add(row);
                }

                row.SetData(entries[i], i, defaultAvatarSprite);
            }

            // Hide any pooled rows that exceed current entry count
            for (int i = count; i < pooledRows.Count; i++)
            {
                pooledRows[i].gameObject.SetActive(false);
            }

            // Scroll to top
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0f, 1f);
            }
        }

        private void EnsureRowPrefab()
        {
            if (rowPrefab != null) return;

            var rowGo = new GameObject("LeaderboardRowPrefab", typeof(RectTransform), typeof(LeaderboardRowUI));
            rowGo.SetActive(false);
            rowGo.transform.SetParent(transform, false);
            rowPrefab = rowGo.GetComponent<LeaderboardRowUI>();
            rowPrefab.EnsureComponents();
        }

        private void ResetPlayerBanner(LeaderboardEntryData playerEntry)
        {
            if (playerBannerContainer == null) return;

            string myUsername = LeaderboardManager.HasInstance
                ? LeaderboardManager.Instance.CurrentPlayerUsername
                : "Player";

            if (playerUsernameText != null)
                playerUsernameText.text = myUsername;

            if (playerAvatarImage != null && defaultAvatarSprite != null)
                playerAvatarImage.sprite = defaultAvatarSprite;

            if (playerEntry != null && playerEntry.CompletionTime > 0f)
            {
                if (playerRankText != null)
                    playerRankText.text = $"#{playerEntry.Rank}";

                if (playerBestTimeText != null)
                    playerBestTimeText.text = LeaderboardUtils.FormatTime(playerEntry.CompletionTime);
            }
            else
            {
                // Check local finished levels as fallback
                if (LevelsManager.HasInstance && LevelsManager.Instance.TryGetBestTime(currentLevelId, out PlayTime localBest))
                {
                    if (playerRankText != null)
                        playerRankText.text = "--";

                    if (playerBestTimeText != null)
                        playerBestTimeText.text = $"{localBest.minutes}m {localBest.seconds:F1}s";
                }
                else
                {
                    if (playerRankText != null)
                        playerRankText.text = "--";

                    if (playerBestTimeText != null)
                        playerBestTimeText.text = "Chưa có thành tích";
                }
            }
        }

        private void ShowLoading()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(true);
            if (emptyStateView != null) emptyStateView.SetActive(false);
            if (errorView != null) errorView.SetActive(false);
            if (scrollRect != null) scrollRect.gameObject.SetActive(false);
        }

        private void ShowEmpty()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (emptyStateView != null) emptyStateView.SetActive(true);
            if (errorView != null) errorView.SetActive(false);
            if (scrollRect != null) scrollRect.gameObject.SetActive(false);
        }

        private void ShowError(string error)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (emptyStateView != null) emptyStateView.SetActive(false);
            if (errorView != null) errorView.SetActive(true);
            if (errorMessageText != null) errorMessageText.text = error;
            if (scrollRect != null) scrollRect.gameObject.SetActive(false);
        }

        private void ShowContent()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (emptyStateView != null) emptyStateView.SetActive(false);
            if (errorView != null) errorView.SetActive(false);
            if (scrollRect != null) scrollRect.gameObject.SetActive(true);
        }

        /// <summary>
        /// Automatically constructs any missing UI elements inside panelRoot
        /// to ensure robust out-of-the-box presentation and styling.
        /// </summary>
        private void BuildUIIfMissing()
        {
            if (panelRoot == null) panelRoot = gameObject;

            var rootRect = panelRoot.GetComponent<RectTransform>();
            if (rootRect == null) return;

            // 1. Background Overlay (if missing)
            if (backgroundOverlayButton == null)
            {
                var overlay = transform.Find("Overlay")?.gameObject;
                if (overlay == null)
                {
                    overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image), typeof(Button));
                    overlay.transform.SetParent(panelRoot.transform, false);
                    overlay.transform.SetAsFirstSibling();

                    var oRect = overlay.GetComponent<RectTransform>();
                    oRect.anchorMin = Vector2.zero;
                    oRect.anchorMax = Vector2.one;
                    oRect.sizeDelta = Vector2.zero;

                    var img = overlay.GetComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0.7f);
                }
                backgroundOverlayButton = overlay.GetComponent<Button>();
            }

            // 2. Main Window
            var window = transform.Find("Window")?.gameObject;
            if (window == null)
            {
                window = new GameObject("Window", typeof(RectTransform), typeof(Image));
                window.transform.SetParent(panelRoot.transform, false);

                var wRect = window.GetComponent<RectTransform>();
                wRect.anchorMin = new Vector2(0.5f, 0.5f);
                wRect.anchorMax = new Vector2(0.5f, 0.5f);
                wRect.pivot = new Vector2(0.5f, 0.5f);
                wRect.sizeDelta = new Vector2(920f, 1500f);

                var wImg = window.GetComponent<Image>();
                wImg.color = new Color(0.97f, 0.94f, 0.88f, 1f); // Warm cozy cream
            }

            // 3. Header
            if (titleText == null)
            {
                var titleGo = window.transform.Find("TitleText")?.gameObject;
                if (titleGo == null)
                {
                    titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGo.transform.SetParent(window.transform, false);
                    var tRect = titleGo.GetComponent<RectTransform>();
                    tRect.anchorMin = new Vector2(0.5f, 1f);
                    tRect.anchorMax = new Vector2(0.5f, 1f);
                    tRect.pivot = new Vector2(0.5f, 1f);
                    tRect.anchoredPosition = new Vector2(0f, -40f);
                    tRect.sizeDelta = new Vector2(600f, 70f);
                }
                titleText = titleGo.GetComponent<TextMeshProUGUI>();
                titleText.text = "BẢNG XẾP HẠNG";
                titleText.fontSize = 42f;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(0.35f, 0.22f, 0.12f);
            }

            // Level Selector Row
            if (levelNameText == null)
            {
                var selectorGo = window.transform.Find("LevelSelector")?.gameObject;
                if (selectorGo == null)
                {
                    selectorGo = new GameObject("LevelSelector", typeof(RectTransform));
                    selectorGo.transform.SetParent(window.transform, false);
                    var sRect = selectorGo.GetComponent<RectTransform>();
                    sRect.anchorMin = new Vector2(0.5f, 1f);
                    sRect.anchorMax = new Vector2(0.5f, 1f);
                    sRect.pivot = new Vector2(0.5f, 1f);
                    sRect.anchoredPosition = new Vector2(0f, -120f);
                    sRect.sizeDelta = new Vector2(500f, 60f);

                    // Prev Button
                    var prevGo = new GameObject("PrevBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    prevGo.transform.SetParent(selectorGo.transform, false);
                    var pRect = prevGo.GetComponent<RectTransform>();
                    pRect.anchorMin = new Vector2(0f, 0.5f);
                    pRect.anchorMax = new Vector2(0f, 0.5f);
                    pRect.pivot = new Vector2(0f, 0.5f);
                    pRect.sizeDelta = new Vector2(60f, 50f);
                    prevGo.GetComponent<Image>().color = new Color(0.7f, 0.55f, 0.35f);
                    var pText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pText.transform.SetParent(prevGo.transform, false);
                    var pt = pText.GetComponent<TextMeshProUGUI>();
                    pt.text = "◄";
                    pt.alignment = TextAlignmentOptions.Center;
                    pt.fontSize = 24f;
                    prevLevelButton = prevGo.GetComponent<Button>();

                    // Level Name Text
                    var nameGo = new GameObject("LevelName", typeof(RectTransform), typeof(TextMeshProUGUI));
                    nameGo.transform.SetParent(selectorGo.transform, false);
                    var nRect = nameGo.GetComponent<RectTransform>();
                    nRect.anchorMin = new Vector2(0.5f, 0.5f);
                    nRect.anchorMax = new Vector2(0.5f, 0.5f);
                    nRect.pivot = new Vector2(0.5f, 0.5f);
                    nRect.sizeDelta = new Vector2(300f, 50f);
                    levelNameText = nameGo.GetComponent<TextMeshProUGUI>();
                    levelNameText.text = "Level 1";
                    levelNameText.alignment = TextAlignmentOptions.Center;
                    levelNameText.fontSize = 32f;
                    levelNameText.fontStyle = FontStyles.Bold;
                    levelNameText.color = new Color(0.35f, 0.22f, 0.12f);

                    // Next Button
                    var nextGo = new GameObject("NextBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    nextGo.transform.SetParent(selectorGo.transform, false);
                    var nxRect = nextGo.GetComponent<RectTransform>();
                    nxRect.anchorMin = new Vector2(1f, 0.5f);
                    nxRect.anchorMax = new Vector2(1f, 0.5f);
                    nxRect.pivot = new Vector2(1f, 0.5f);
                    nxRect.sizeDelta = new Vector2(60f, 50f);
                    nextGo.GetComponent<Image>().color = new Color(0.7f, 0.55f, 0.35f);
                    var nxText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    nxText.transform.SetParent(nextGo.transform, false);
                    var nxt = nxText.GetComponent<TextMeshProUGUI>();
                    nxt.text = "►";
                    nxt.alignment = TextAlignmentOptions.Center;
                    nxt.fontSize = 24f;
                    nextLevelButton = nextGo.GetComponent<Button>();
                }
            }

            // Close Button
            if (closeButton == null)
            {
                var closeGo = window.transform.Find("CloseBtn")?.gameObject;
                if (closeGo == null)
                {
                    closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    closeGo.transform.SetParent(window.transform, false);
                    var cRect = closeGo.GetComponent<RectTransform>();
                    cRect.anchorMin = new Vector2(1f, 1f);
                    cRect.anchorMax = new Vector2(1f, 1f);
                    cRect.pivot = new Vector2(1f, 1f);
                    cRect.anchoredPosition = new Vector2(-25f, -25f);
                    cRect.sizeDelta = new Vector2(65f, 65f);
                    closeGo.GetComponent<Image>().color = new Color(0.85f, 0.3f, 0.25f);

                    var xText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    xText.transform.SetParent(closeGo.transform, false);
                    var xt = xText.GetComponent<TextMeshProUGUI>();
                    xt.text = "✕";
                    xt.alignment = TextAlignmentOptions.Center;
                    xt.fontSize = 32f;
                    xt.color = Color.white;
                }
                closeButton = closeGo.GetComponent<Button>();
            }

            // 4. Player Best Record Banner
            if (playerBannerContainer == null)
            {
                playerBannerContainer = window.transform.Find("PlayerBanner")?.gameObject;
                if (playerBannerContainer == null)
                {
                    playerBannerContainer = new GameObject("PlayerBanner", typeof(RectTransform), typeof(Image));
                    playerBannerContainer.transform.SetParent(window.transform, false);
                    var pbRect = playerBannerContainer.GetComponent<RectTransform>();
                    pbRect.anchorMin = new Vector2(0.5f, 1f);
                    pbRect.anchorMax = new Vector2(0.5f, 1f);
                    pbRect.pivot = new Vector2(0.5f, 1f);
                    pbRect.anchoredPosition = new Vector2(0f, -200f);
                    pbRect.sizeDelta = new Vector2(840f, 100f);
                    playerBannerContainer.GetComponent<Image>().color = new Color(0.92f, 0.88f, 0.76f, 1f);

                    var layout = playerBannerContainer.AddComponent<HorizontalLayoutGroup>();
                    layout.childControlWidth = false;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = true;
                    layout.spacing = 15f;
                    layout.padding = new RectOffset(20, 20, 10, 10);

                    // Rank
                    var rGo = new GameObject("Rank", typeof(RectTransform), typeof(TextMeshProUGUI));
                    rGo.transform.SetParent(playerBannerContainer.transform, false);
                    rGo.GetComponent<RectTransform>().sizeDelta = new Vector2(90f, 0f);
                    playerRankText = rGo.GetComponent<TextMeshProUGUI>();
                    playerRankText.text = "#--";
                    playerRankText.fontSize = 30f;
                    playerRankText.fontStyle = FontStyles.Bold;
                    playerRankText.alignment = TextAlignmentOptions.Center;
                    playerRankText.color = new Color(0.35f, 0.22f, 0.12f);

                    // Avatar
                    var avGo = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
                    avGo.transform.SetParent(playerBannerContainer.transform, false);
                    avGo.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
                    playerAvatarImage = avGo.GetComponent<Image>();
                    playerAvatarImage.color = new Color(0.7f, 0.7f, 0.7f);

                    // Username
                    var unGo = new GameObject("Username", typeof(RectTransform), typeof(TextMeshProUGUI));
                    unGo.transform.SetParent(playerBannerContainer.transform, false);
                    unGo.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 0f);
                    playerUsernameText = unGo.GetComponent<TextMeshProUGUI>();
                    playerUsernameText.text = "Bạn";
                    playerUsernameText.fontSize = 26f;
                    playerUsernameText.fontStyle = FontStyles.Bold;
                    playerUsernameText.alignment = TextAlignmentOptions.Left;
                    playerUsernameText.color = new Color(0.2f, 0.45f, 0.2f);

                    // Best time
                    var tmGo = new GameObject("BestTime", typeof(RectTransform), typeof(TextMeshProUGUI));
                    tmGo.transform.SetParent(playerBannerContainer.transform, false);
                    tmGo.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, 0f);
                    playerBestTimeText = tmGo.GetComponent<TextMeshProUGUI>();
                    playerBestTimeText.text = "--:--";
                    playerBestTimeText.fontSize = 26f;
                    playerBestTimeText.fontStyle = FontStyles.Bold;
                    playerBestTimeText.alignment = TextAlignmentOptions.Right;
                    playerBestTimeText.color = new Color(0.35f, 0.22f, 0.12f);
                }
            }

            // 5. Scroll View for rows
            if (scrollRect == null)
            {
                var scrollGo = window.transform.Find("ScrollView")?.gameObject;
                if (scrollGo == null)
                {
                    scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                    scrollGo.transform.SetParent(window.transform, false);
                    var scRect = scrollGo.GetComponent<RectTransform>();
                    scRect.anchorMin = new Vector2(0.5f, 0.5f);
                    scRect.anchorMax = new Vector2(0.5f, 0.5f);
                    scRect.pivot = new Vector2(0.5f, 0.5f);
                    scRect.anchoredPosition = new Vector2(0f, -120f);
                    scRect.sizeDelta = new Vector2(840f, 1000f);

                    scrollGo.GetComponent<Image>().color = new Color(0.92f, 0.89f, 0.82f, 0.5f);
                    scrollRect = scrollGo.GetComponent<ScrollRect>();
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;

                    // Viewport
                    var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
                    vpGo.transform.SetParent(scrollGo.transform, false);
                    var vpRect = vpGo.GetComponent<RectTransform>();
                    vpRect.anchorMin = Vector2.zero;
                    vpRect.anchorMax = Vector2.one;
                    vpRect.sizeDelta = Vector2.zero;
                    vpGo.GetComponent<Mask>().showMaskGraphic = false;

                    // Content
                    var cGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                    cGo.transform.SetParent(vpGo.transform, false);
                    var ctRect = cGo.GetComponent<RectTransform>();
                    ctRect.anchorMin = new Vector2(0f, 1f);
                    ctRect.anchorMax = new Vector2(1f, 1f);
                    ctRect.pivot = new Vector2(0.5f, 1f);
                    ctRect.anchoredPosition = Vector2.zero;
                    ctRect.sizeDelta = new Vector2(0f, 0f);

                    var vLayout = cGo.GetComponent<VerticalLayoutGroup>();
                    vLayout.childControlWidth = true;
                    vLayout.childControlHeight = false;
                    vLayout.childForceExpandWidth = true;
                    vLayout.childForceExpandHeight = false;
                    vLayout.spacing = 8f;
                    vLayout.padding = new RectOffset(10, 10, 10, 10);

                    var fitter = cGo.GetComponent<ContentSizeFitter>();
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    scrollRect.viewport = vpRect;
                    scrollRect.content = ctRect;
                    rowsContainer = cGo.transform;
                }
            }

            // 6. Loading Indicator
            if (loadingIndicator == null)
            {
                loadingIndicator = window.transform.Find("LoadingIndicator")?.gameObject;
                if (loadingIndicator == null)
                {
                    loadingIndicator = new GameObject("LoadingIndicator", typeof(RectTransform), typeof(TextMeshProUGUI));
                    loadingIndicator.transform.SetParent(window.transform, false);
                    var lRect = loadingIndicator.GetComponent<RectTransform>();
                    lRect.anchorMin = new Vector2(0.5f, 0.5f);
                    lRect.anchorMax = new Vector2(0.5f, 0.5f);
                    lRect.anchoredPosition = new Vector2(0f, -120f);
                    lRect.sizeDelta = new Vector2(600f, 100f);

                    var lt = loadingIndicator.GetComponent<TextMeshProUGUI>();
                    lt.text = "Đang tải bảng xếp hạng...";
                    lt.fontSize = 32f;
                    lt.alignment = TextAlignmentOptions.Center;
                    lt.color = new Color(0.45f, 0.35f, 0.25f);
                }
            }

            // 7. Empty State View
            if (emptyStateView == null)
            {
                emptyStateView = window.transform.Find("EmptyState")?.gameObject;
                if (emptyStateView == null)
                {
                    emptyStateView = new GameObject("EmptyState", typeof(RectTransform), typeof(TextMeshProUGUI));
                    emptyStateView.transform.SetParent(window.transform, false);
                    var eRect = emptyStateView.GetComponent<RectTransform>();
                    eRect.anchorMin = new Vector2(0.5f, 0.5f);
                    eRect.anchorMax = new Vector2(0.5f, 0.5f);
                    eRect.anchoredPosition = new Vector2(0f, -120f);
                    eRect.sizeDelta = new Vector2(600f, 120f);

                    var et = emptyStateView.GetComponent<TextMeshProUGUI>();
                    et.text = "Chưa có dữ liệu xếp hạng cho màn này.\nHãy là người đầu tiên chinh phục!";
                    et.fontSize = 28f;
                    et.alignment = TextAlignmentOptions.Center;
                    et.color = new Color(0.5f, 0.4f, 0.3f);
                }
            }

            // 8. Error View
            if (errorView == null)
            {
                errorView = window.transform.Find("ErrorView")?.gameObject;
                if (errorView == null)
                {
                    errorView = new GameObject("ErrorView", typeof(RectTransform));
                    errorView.transform.SetParent(window.transform, false);
                    var errRect = errorView.GetComponent<RectTransform>();
                    errRect.anchorMin = new Vector2(0.5f, 0.5f);
                    errRect.anchorMax = new Vector2(0.5f, 0.5f);
                    errRect.anchoredPosition = new Vector2(0f, -120f);
                    errRect.sizeDelta = new Vector2(600f, 200f);

                    var msgGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
                    msgGo.transform.SetParent(errorView.transform, false);
                    var mRect = msgGo.GetComponent<RectTransform>();
                    mRect.anchorMin = new Vector2(0.5f, 0.7f);
                    mRect.anchorMax = new Vector2(0.5f, 0.7f);
                    mRect.sizeDelta = new Vector2(560f, 100f);
                    errorMessageText = msgGo.GetComponent<TextMeshProUGUI>();
                    errorMessageText.text = "Không thể tải dữ liệu.";
                    errorMessageText.fontSize = 26f;
                    errorMessageText.alignment = TextAlignmentOptions.Center;
                    errorMessageText.color = new Color(0.85f, 0.3f, 0.25f);

                    var retGo = new GameObject("RetryBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    retGo.transform.SetParent(errorView.transform, false);
                    var rRect = retGo.GetComponent<RectTransform>();
                    rRect.anchorMin = new Vector2(0.5f, 0.2f);
                    rRect.anchorMax = new Vector2(0.5f, 0.2f);
                    rRect.sizeDelta = new Vector2(220f, 65f);
                    retGo.GetComponent<Image>().color = new Color(0.4f, 0.7f, 0.4f);

                    var rTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    rTxt.transform.SetParent(retGo.transform, false);
                    var rt = rTxt.GetComponent<TextMeshProUGUI>();
                    rt.text = "Thử lại";
                    rt.alignment = TextAlignmentOptions.Center;
                    rt.fontSize = 26f;
                    rt.color = Color.white;

                    retryButton = retGo.GetComponent<Button>();
                }
            }

            EnsureRowPrefab();
        }
    }
}
