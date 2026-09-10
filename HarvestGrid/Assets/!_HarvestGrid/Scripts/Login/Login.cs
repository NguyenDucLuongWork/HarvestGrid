using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HarvestGrid.Managers;

using PlayFab;
using PlayFab.ClientModels;

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

            string email = emailInput.text.Trim();
            string password = passwordInput.text;

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                ShowMessage("Vui lòng nhập email hợp lệ.", Color.red);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowMessage("Vui lòng nhập mật khẩu.", Color.red);
                return;
            }

            SetSubmitting(true);
            ShowMessage("Đang kết nối tới PlayFab...", new Color(1f, 0.75f, 0.2f));

            if (AuthManager.Instance == null)
            {
                ShowMessage("Lỗi hệ thống: Không tìm thấy AuthManager.", Color.red);
                SetSubmitting(false);
                return;
            }

            // Gọi logic Đăng nhập/Đăng ký tự động qua AuthManager
            AuthManager.Instance.LoginPlayFab(email, password, OnLoginSuccess, OnLoginError);
        }

        private void OnLoginSuccess(LoginResult result)
        {
            if (isDestroyed) return;

            ShowMessage("Đăng nhập thành công!", new Color(0.2f, 0.8f, 0.3f));

            if (!string.IsNullOrWhiteSpace(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private void OnLoginError(PlayFabError error)
        {
            if (isDestroyed) return;

            Debug.LogError($"[PlayFab] Error: {error.GenerateErrorReport()}");
            ShowMessage(GetErrorMessage(error), Color.red);
            SetSubmitting(false);
        }

        private static string GetErrorMessage(PlayFabError error)
        {
            switch (error.Error)
            {
                case PlayFabErrorCode.InvalidEmailAddress:
                    return "Email không hợp lệ.";
                case PlayFabErrorCode.InvalidEmailOrPassword:
                case PlayFabErrorCode.AccountNotFound:
                    return "Email hoặc mật khẩu không đúng.";
                case PlayFabErrorCode.AccountBanned:
                    return "Tài khoản này đã bị vô hiệu hóa.";
                case PlayFabErrorCode.ConnectionError:
                    return "Không thể kết nối Server. Hãy kiểm tra mạng.";
                case PlayFabErrorCode.ServiceUnavailable:
                    return "Server đang bảo trì. Vui lòng thử lại sau.";
                default:
                    return $"Lỗi Đăng nhập: {error.ErrorMessage}";
            }
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
