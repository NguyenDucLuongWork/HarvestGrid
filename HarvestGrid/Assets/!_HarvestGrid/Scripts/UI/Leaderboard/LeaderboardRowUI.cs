using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestGrid.Leaderboard
{
    public class LeaderboardRowUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private Image rankBadgeImage;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image backgroundImage;

        [Header("Rank Colors (Cozy Palette)")]
        [SerializeField] private Color top1Color = new Color(0.95f, 0.76f, 0.20f);    // Gold / Amber
        [SerializeField] private Color top2Color = new Color(0.75f, 0.78f, 0.82f);    // Silver
        [SerializeField] private Color top3Color = new Color(0.80f, 0.52f, 0.25f);    // Bronze
        [SerializeField] private Color defaultRankColor = new Color(0.35f, 0.25f, 0.18f); // Earth brown

        [Header("Row Highlight")]
        [SerializeField] private Color currentPlayerRowColor = new Color(0.90f, 0.98f, 0.82f, 0.95f); // Soft farm green highlight
        [SerializeField] private Color normalRowColor = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color alternateRowColor = new Color(0.95f, 0.93f, 0.88f, 0.65f);

        [Header("Top Badges (Optional)")]
        [SerializeField] private Sprite crownOrTop1Sprite;
        [SerializeField] private Sprite top2Sprite;
        [SerializeField] private Sprite top3Sprite;

        private void Awake()
        {
            EnsureComponents();
        }

        public void EnsureComponents()
        {
            var rect = GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.y <= 0f)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 85f);
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                if (backgroundImage == null)
                    backgroundImage = gameObject.AddComponent<Image>();
            }

            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                layout.spacing = 15f;
                layout.padding = new RectOffset(20, 20, 10, 10);
            }

            // Rank Text
            if (rankText == null)
            {
                var rankGo = transform.Find("RankText")?.gameObject;
                if (rankGo == null)
                {
                    rankGo = new GameObject("RankText", typeof(RectTransform));
                    rankGo.transform.SetParent(transform, false);
                }
                var rankRect = rankGo.GetComponent<RectTransform>();
                rankRect.sizeDelta = new Vector2(70f, 0f);
                rankText = rankGo.GetComponent<TextMeshProUGUI>();
                if (rankText == null) rankText = rankGo.AddComponent<TextMeshProUGUI>();
                rankText.alignment = TextAlignmentOptions.Center;
                rankText.fontSize = 28f;
                rankText.fontStyle = FontStyles.Bold;
            }

            // Avatar Image
            if (avatarImage == null)
            {
                var avatarGo = transform.Find("AvatarImage")?.gameObject;
                if (avatarGo == null)
                {
                    avatarGo = new GameObject("AvatarImage", typeof(RectTransform));
                    avatarGo.transform.SetParent(transform, false);
                }
                var avatarRect = avatarGo.GetComponent<RectTransform>();
                avatarRect.sizeDelta = new Vector2(60f, 60f);
                avatarImage = avatarGo.GetComponent<Image>();
                if (avatarImage == null) avatarImage = avatarGo.AddComponent<Image>();
            }

            // Username Text
            if (usernameText == null)
            {
                var userGo = transform.Find("UsernameText")?.gameObject;
                if (userGo == null)
                {
                    userGo = new GameObject("UsernameText", typeof(RectTransform));
                    userGo.transform.SetParent(transform, false);
                }
                var userRect = userGo.GetComponent<RectTransform>();
                userRect.sizeDelta = new Vector2(340f, 0f);
                usernameText = userGo.GetComponent<TextMeshProUGUI>();
                if (usernameText == null) usernameText = userGo.AddComponent<TextMeshProUGUI>();
                usernameText.alignment = TextAlignmentOptions.Left;
                usernameText.fontSize = 24f;
                usernameText.color = defaultRankColor;
                usernameText.overflowMode = TextOverflowModes.Ellipsis;
            }

            // Time Text
            if (timeText == null)
            {
                var timeGo = transform.Find("TimeText")?.gameObject;
                if (timeGo == null)
                {
                    timeGo = new GameObject("TimeText", typeof(RectTransform));
                    timeGo.transform.SetParent(transform, false);
                }
                var timeRect = timeGo.GetComponent<RectTransform>();
                timeRect.sizeDelta = new Vector2(180f, 0f);
                timeText = timeGo.GetComponent<TextMeshProUGUI>();
                if (timeText == null) timeText = timeGo.AddComponent<TextMeshProUGUI>();
                timeText.alignment = TextAlignmentOptions.Right;
                timeText.fontSize = 26f;
                timeText.fontStyle = FontStyles.Bold;
                timeText.color = defaultRankColor;
            }
        }

        public void SetData(LeaderboardEntryData data, int rowIndex, Sprite fallbackAvatar = null)
        {
            EnsureComponents();

            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // 1. Rank & Rank Badge / Color
            if (rankText != null)
            {
                rankText.text = $"#{data.Rank}";
                switch (data.Rank)
                {
                    case 1:
                        rankText.color = top1Color;
                        break;
                    case 2:
                        rankText.color = top2Color;
                        break;
                    case 3:
                        rankText.color = top3Color;
                        break;
                    default:
                        rankText.color = defaultRankColor;
                        break;
                }
            }

            if (rankBadgeImage != null)
            {
                switch (data.Rank)
                {
                    case 1:
                        rankBadgeImage.gameObject.SetActive(true);
                        rankBadgeImage.color = top1Color;
                        if (crownOrTop1Sprite != null) rankBadgeImage.sprite = crownOrTop1Sprite;
                        break;
                    case 2:
                        rankBadgeImage.gameObject.SetActive(true);
                        rankBadgeImage.color = top2Color;
                        if (top2Sprite != null) rankBadgeImage.sprite = top2Sprite;
                        break;
                    case 3:
                        rankBadgeImage.gameObject.SetActive(true);
                        rankBadgeImage.color = top3Color;
                        if (top3Sprite != null) rankBadgeImage.sprite = top3Sprite;
                        break;
                    default:
                        rankBadgeImage.gameObject.SetActive(false);
                        break;
                }
            }

            // 2. Avatar
            if (avatarImage != null && fallbackAvatar != null)
            {
                avatarImage.sprite = fallbackAvatar;
            }

            // 3. Username
            if (usernameText != null)
            {
                usernameText.text = data.DisplayName;
                usernameText.fontStyle = data.IsCurrentPlayer ? FontStyles.Bold : FontStyles.Normal;
            }

            // 4. Completion Time
            if (timeText != null)
            {
                timeText.text = LeaderboardUtils.FormatTime(data.CompletionTime);
            }

            // 5. Row Background (Highlight local player or zebra-stripe)
            if (backgroundImage != null)
            {
                if (data.IsCurrentPlayer)
                {
                    backgroundImage.color = currentPlayerRowColor;
                }
                else
                {
                    backgroundImage.color = (rowIndex % 2 == 0) ? normalRowColor : alternateRowColor;
                }
            }
        }
    }
}
