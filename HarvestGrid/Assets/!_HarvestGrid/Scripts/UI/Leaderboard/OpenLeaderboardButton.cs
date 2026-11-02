using UnityEngine;
using UnityEngine.UI;

namespace HarvestGrid.Leaderboard
{
    [RequireComponent(typeof(Button))]
    public class OpenLeaderboardButton : MonoBehaviour
    {
        [Tooltip("Optional reference to LeaderboardUI. If not assigned, searches scene.")]
        [SerializeField] private LeaderboardUI leaderboardUI;

        [Tooltip("Optional specific level to view. If null or negative, uses default.")]
        [SerializeField] private int targetLevelId = -1;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }

        public void SetTargetLevel(int levelId)
        {
            targetLevelId = levelId;
        }

        private void OnClick()
        {
            if (leaderboardUI == null)
            {
                leaderboardUI = FindAnyObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
            }

            if (leaderboardUI != null)
            {
                if (targetLevelId >= 0)
                {
                    leaderboardUI.Open(targetLevelId);
                }
                else
                {
                    leaderboardUI.Open();
                }
            }
            else
            {
                Debug.LogWarning("[OpenLeaderboardButton] LeaderboardUI not found in scene!");
            }
        }
    }
}

