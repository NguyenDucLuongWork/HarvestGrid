using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HarvestGrid.Managers;
using HarvestGrid.Managers.Auth; // Dùng IAuthService và AuthResult mới

namespace HarvestGrid.UI
{
    public class LoginUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private string nextSceneName = "HomeScene";

        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private GameObject loadingIndicator;

        private bool isSubmitting;
        private bool hasRuntimeLoginListener;
        private bool isDestroyed;

        private void Awake()
        {
            if (messageText == null) CreateMessageText();

            if (loginButton.onClick.GetPersistentEventCount() == 0)
            {
                loginButton.onClick.AddListener(Login);
                hasRuntimeLoginListener = true;
            }

            passwordInput.onSubmit.AddListener(OnPasswordSubmitted);
            SetSubmitting(false);
            ShowMessage(string.Empty, Color.white);
        }

        private void OnDestroy()
        {
            isDestroyed = true;

            if (hasRuntimeLoginListener)
                loginButton.onClick.RemoveListener(Login);

            passwordInput.onSubmit.RemoveListener(OnPasswordSubmitted);
        }

        private void OnPasswordSubmitted(string _)
        {
            Login();
        }

        public void Login()
        {
            if (isSubmitting) return;

            string username = emailInput.text.Trim();
            string password = passwordInput.text;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowMessage("Vui lòng nhập Username hợp lệ.", new Color(0.9f, 0.3f, 0.3f));
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowMessage("Vui lòng nhập Mật khẩu.", new Color(0.9f, 0.3f, 0.3f));
                return;
            }

            SetSubmitting(true);
            ShowMessage(string.Empty, Color.white);

            if (AuthManager.Instance == null || AuthManager.Instance.AuthService == null)
            {
                ShowMessage("Lỗi hệ thống: Không tìm thấy AuthManager.", new Color(0.9f, 0.3f, 0.3f));
                SetSubmitting(false);
                return;
            }

            // Gọi logic Đăng nhập thông qua lớp Interface trung gian
            AuthManager.Instance.AuthService.Login(username, password, OnLoginSuccess, OnLoginError);
        }

        private void OnLoginSuccess(AuthResult result)
        {
            if (isDestroyed) return;

            ShowMessage($"Đăng nhập thành công! Chào {result.Username}...", new Color(0.3f, 0.7f, 0.3f));

            if (!string.IsNullOrWhiteSpace(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private void OnLoginError(string errorMessage)
        {
            if (isDestroyed) return;

            Debug.LogError($"[Auth] Error: {errorMessage}");
            ShowMessage(errorMessage, new Color(0.9f, 0.3f, 0.3f));
            SetSubmitting(false);
        }

        private void SetSubmitting(bool value)
        {
            isSubmitting = value;
            if (loginButton != null) loginButton.interactable = !value;
            if (emailInput != null) emailInput.interactable = !value;
            if (passwordInput != null) passwordInput.interactable = !value;
            
            if (loadingIndicator != null) loadingIndicator.SetActive(value);
        }

        private void ShowMessage(string message, Color color)
        {
            messageText.text = message;
            messageText.color = color;
        }

        private void CreateMessageText()
        {
            var messageObject = new GameObject("LoginMessage", typeof(RectTransform));
            messageObject.transform.SetParent(transform, false);

            var rect = messageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -185f);
            rect.sizeDelta = new Vector2(500f, 50f);

            messageText = messageObject.AddComponent<TextMeshProUGUI>();
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.fontSize = 22f;
            messageText.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}
