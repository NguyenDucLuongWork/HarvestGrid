using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace HarvestGrid.Leaderboard
{
    public class HomeSceneLeaderboardIntegration : MonoBehaviour
    {
        [Header("Leaderboard Panel Reference")]
        [SerializeField] private LeaderboardUI leaderboardUI;

        [Header("Optional Button References")]
        [SerializeField] private Button homeLeaderboardButton;
        [SerializeField] private Button levelPanelLeaderboardButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadListener()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CheckInitialScene()
        {
            if (SceneManager.GetActiveScene().name == "HomeScene")
            {
                CheckAndCreateIntegration();
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "HomeScene")
            {
                CheckAndCreateIntegration();
            }
        }

        private static void CheckAndCreateIntegration()
        {
            var existing = FindAnyObjectByType<HomeSceneLeaderboardIntegration>();
            if (existing == null)
            {
                var go = new GameObject("HomeSceneLeaderboardIntegration");
                go.AddComponent<HomeSceneLeaderboardIntegration>();
            }
        }

        private void Start()
        {
            EnsureLeaderboardUI();
            SetupHomeLeaderboardButton();
            SetupLevelPanelLeaderboardButton();
        }

        private void EnsureLeaderboardUI()
        {
            if (leaderboardUI == null)
            {
                leaderboardUI = FindAnyObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
            }

            if (leaderboardUI == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    GameObject lbGo = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(LeaderboardUI));
                    lbGo.transform.SetParent(canvas.transform, false);
                    leaderboardUI = lbGo.GetComponent<LeaderboardUI>();
                    leaderboardUI.Initialize();
                    lbGo.SetActive(false);
                    Debug.Log("[HomeSceneLeaderboardIntegration] Created LeaderboardUI under Canvas.");
                }
            }
        }

        private void SetupHomeLeaderboardButton()
        {
            if (homeLeaderboardButton != null)
            {
                homeLeaderboardButton.onClick.RemoveListener(OpenLeaderboard);
                homeLeaderboardButton.onClick.AddListener(OpenLeaderboard);
                return;
            }

            Canvas canvas = FindAnyObjectByType<Canvas>();
            Transform homePanelTransform = canvas != null
                ? FindChildRecursive(canvas.transform, "HomePanel")
                : GameObject.Find("HomePanel")?.transform;

            if (homePanelTransform == null) return;

            var existingBtn = homePanelTransform.Find("LeaderboardBtn")?.GetComponent<Button>();
            if (existingBtn != null)
            {
                homeLeaderboardButton = existingBtn;
                homeLeaderboardButton.onClick.RemoveListener(OpenLeaderboard);
                homeLeaderboardButton.onClick.AddListener(OpenLeaderboard);
                return;
            }

            // Create Leaderboard button on HomePanel
            var btnGo = new GameObject("LeaderboardBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(homePanelTransform, false);

            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 250f); // Positioned nicely above PlayBtn (y = 114.8)
            rect.sizeDelta = new Vector2(350f, 90f);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.96f, 0.78f, 0.28f); // Golden amber button

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "BẢNG XẾP HẠNG";
            tmp.fontSize = 28f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.35f, 0.2f, 0.08f);

            homeLeaderboardButton = btnGo.GetComponent<Button>();
            homeLeaderboardButton.onClick.AddListener(OpenLeaderboard);
        }

        private void SetupLevelPanelLeaderboardButton()
        {
            if (levelPanelLeaderboardButton != null)
            {
                levelPanelLeaderboardButton.onClick.RemoveListener(OpenLeaderboard);
                levelPanelLeaderboardButton.onClick.AddListener(OpenLeaderboard);
                return;
            }

            Canvas canvas = FindAnyObjectByType<Canvas>();
            Transform levelPanelTransform = canvas != null
                ? FindChildRecursive(canvas.transform, "LevelPanel")
                : GameObject.Find("LevelPanel")?.transform;

            if (levelPanelTransform == null) return;

            var existingBtn = levelPanelTransform.Find("LevelLeaderboardBtn")?.GetComponent<Button>();
            if (existingBtn != null)
            {
                levelPanelLeaderboardButton = existingBtn;
                levelPanelLeaderboardButton.onClick.RemoveListener(OpenLeaderboard);
                levelPanelLeaderboardButton.onClick.AddListener(OpenLeaderboard);
                return;
            }

            // Create button on LevelPanel header
            var btnGo = new GameObject("LevelLeaderboardBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(levelPanelTransform, false);

            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -220f);
            rect.sizeDelta = new Vector2(300f, 75f);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.42f, 0.68f, 0.42f); // Farm green

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "BẢNG XẾP HẠNG";
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            levelPanelLeaderboardButton = btnGo.GetComponent<Button>();
            levelPanelLeaderboardButton.onClick.AddListener(OpenLeaderboard);
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindChildRecursive(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private void OpenLeaderboard()
        {
            if (leaderboardUI == null)
            {
                EnsureLeaderboardUI();
            }

            if (leaderboardUI != null)
            {
                leaderboardUI.Open();
            }
        }
    }
}
