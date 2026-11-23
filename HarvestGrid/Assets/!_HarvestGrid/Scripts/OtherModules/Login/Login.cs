using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HarvestGrid.Managers;
using HarvestGrid.Managers.Auth;

namespace HarvestGrid.UI
{
    public class LoginUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private string nextSceneName = "HomeScene";

        [Header("Panels")]
        [Tooltip("Panel chứa form đăng nhập (mặc định hiển thị)")]
        [SerializeField] private GameObject loginPanel;
        [Tooltip("Panel chứa form đăng ký (mặc định ẩn)")]
        [SerializeField] private GameObject registerPanel;

        [Header("Login UI References")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [Tooltip("Nút chuyển sang giao diện đăng ký")]
        [SerializeField] private Button openRegisterButton;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Register UI References")]
        [SerializeField] private TMP_InputField regUsernameInput;
        [SerializeField] private TMP_InputField regPasswordInput;
        [SerializeField] private TMP_InputField regConfirmPasswordInput;
        [SerializeField] private Button submitRegisterButton;
        [SerializeField] private Button backToLoginButton;
        [SerializeField] private TextMeshProUGUI regMessageText;
        [SerializeField] private GameObject regLoadingIndicator;

        private readonly Color successColor = new Color(0.2f, 0.8f, 0.2f);
        private readonly Color errorColor = new Color(0.95f, 0.3f, 0.3f);

        private bool isSubmitting;
        private bool hasRuntimeLoginListener;
        private bool hasRuntimeOpenRegisterListener;
        private bool hasRuntimeSubmitRegisterListener;
        private bool hasRuntimeBackToLoginListener;
        private bool isDestroyed;

        private void Awake()
        {
            // Thiết lập Password ContentType bảo mật
            if (passwordInput != null)
                passwordInput.contentType = TMP_InputField.ContentType.Password;

            if (regPasswordInput != null)
                regPasswordInput.contentType = TMP_InputField.ContentType.Password;

            if (regConfirmPasswordInput != null)
                regConfirmPasswordInput.contentType = TMP_InputField.ContentType.Password;

            // Đăng ký sự kiện Login
            if (loginButton != null && loginButton.onClick.GetPersistentEventCount() == 0)
            {
                loginButton.onClick.AddListener(Login);
                hasRuntimeLoginListener = true;
            }

            if (passwordInput != null)
                passwordInput.onSubmit.AddListener(OnPasswordSubmitted);

            // Đăng ký sự kiện Register Panel
            if (openRegisterButton != null && openRegisterButton.onClick.GetPersistentEventCount() == 0)
            {
                openRegisterButton.onClick.AddListener(OpenRegisterPanel);
                hasRuntimeOpenRegisterListener = true;
            }

            if (backToLoginButton != null && backToLoginButton.onClick.GetPersistentEventCount() == 0)
            {
                backToLoginButton.onClick.AddListener(OpenLoginPanel);
                hasRuntimeBackToLoginListener = true;
            }

            if (submitRegisterButton != null && submitRegisterButton.onClick.GetPersistentEventCount() == 0)
            {
                submitRegisterButton.onClick.AddListener(Register);
                hasRuntimeSubmitRegisterListener = true;
            }

            if (regConfirmPasswordInput != null)
                regConfirmPasswordInput.onSubmit.AddListener(OnRegConfirmPasswordSubmitted);

            // Trạng thái ban đầu
            SetLoginSubmitting(false);
            SetRegisterSubmitting(false);
            OpenLoginPanel();
            ShowLoginMessage(string.Empty, Color.white);
            ShowRegisterMessage(string.Empty, Color.white);
        }

        private void OnDestroy()
        {
            isDestroyed = true;

            if (hasRuntimeLoginListener && loginButton != null)
                loginButton.onClick.RemoveListener(Login);

            if (passwordInput != null)
                passwordInput.onSubmit.RemoveListener(OnPasswordSubmitted);

            if (hasRuntimeOpenRegisterListener && openRegisterButton != null)
                openRegisterButton.onClick.RemoveListener(OpenRegisterPanel);

            if (hasRuntimeBackToLoginListener && backToLoginButton != null)
                backToLoginButton.onClick.RemoveListener(OpenLoginPanel);

            if (hasRuntimeSubmitRegisterListener && submitRegisterButton != null)
                submitRegisterButton.onClick.RemoveListener(Register);

            if (regConfirmPasswordInput != null)
                regConfirmPasswordInput.onSubmit.RemoveListener(OnRegConfirmPasswordSubmitted);
        }

        #region Navigation & Panel Switching

        public void OpenRegisterPanel()
        {
            if (isSubmitting) return;

            if (loginPanel != null)
                loginPanel.SetActive(false);

            if (openRegisterButton != null)
                openRegisterButton.gameObject.SetActive(false);

            if (registerPanel != null)
                registerPanel.SetActive(true);

            // Reset dữ liệu nhập của Register
            if (regUsernameInput != null) regUsernameInput.text = string.Empty;
            if (regPasswordInput != null) regPasswordInput.text = string.Empty;
            if (regConfirmPasswordInput != null) regConfirmPasswordInput.text = string.Empty;

            ShowRegisterMessage(string.Empty, Color.white);
            SetRegisterSubmitting(false);

            if (regUsernameInput != null)
                regUsernameInput.Select();
        }

        public void OpenLoginPanel()
        {
            if (isSubmitting) return;

            if (registerPanel != null)
                registerPanel.SetActive(false);

            if (loginPanel != null)
                loginPanel.SetActive(true);

            if (openRegisterButton != null)
                openRegisterButton.gameObject.SetActive(true);

            // Xóa trường mật khẩu để đảm bảo an toàn
            if (regPasswordInput != null) regPasswordInput.text = string.Empty;
            if (regConfirmPasswordInput != null) regConfirmPasswordInput.text = string.Empty;

            SetLoginSubmitting(false);
        }

        #endregion

        #region Login Logic

        private void OnPasswordSubmitted(string _)
        {
            Login();
        }

        public void Login()
        {
            if (isSubmitting) return;

            string username = emailInput != null ? emailInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowLoginMessage("Vui lòng nhập tên đăng nhập.", errorColor);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowLoginMessage("Vui lòng nhập mật khẩu.", errorColor);
                return;
            }

            SetLoginSubmitting(true);
            ShowLoginMessage("Đang đăng nhập, vui lòng chờ...", Color.white);

            if (AuthManager.Instance == null || AuthManager.Instance.AuthService == null)
            {
                ShowLoginMessage("Lỗi hệ thống: Không tìm thấy AuthManager.", errorColor);
                SetLoginSubmitting(false);
                return;
            }

            AuthManager.Instance.AuthService.Login(username, password, OnLoginSuccess, OnLoginError);
        }

        private void OnLoginSuccess(AuthResult result)
        {
            if (isDestroyed) return;

            ShowLoginMessage($"Đăng nhập thành công! Chào {result.Username}...", successColor);

            if (!string.IsNullOrWhiteSpace(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private void OnLoginError(string errorMessage)
        {
            if (isDestroyed) return;

            Debug.LogError($"[Auth] Error: {errorMessage}");
            ShowLoginMessage(errorMessage, errorColor);
            SetLoginSubmitting(false);
        }

        #endregion

        #region Register Logic

        private void OnRegConfirmPasswordSubmitted(string _)
        {
            Register();
        }

        public void Register()
        {
            if (isSubmitting) return;

            string rawUsername = regUsernameInput != null ? regUsernameInput.text : string.Empty;
            string username = rawUsername.Trim();
            string password = regPasswordInput != null ? regPasswordInput.text : string.Empty;
            string confirmPassword = regConfirmPasswordInput != null ? regConfirmPasswordInput.text : string.Empty;

            // 1. Validate Username
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowRegisterMessage("Vui lòng nhập tên đăng nhập.", errorColor);
                return;
            }

            if (username.Length < 3 || username.Length > 20)
            {
                ShowRegisterMessage("Tên đăng nhập phải có độ dài từ 3 đến 20 ký tự.", errorColor);
                return;
            }

            if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]+$"))
            {
                ShowRegisterMessage("Tên đăng nhập chỉ được chứa chữ cái, chữ số và dấu gạch dưới (_).", errorColor);
                return;
            }

            // 2. Validate Password
            if (string.IsNullOrEmpty(password))
            {
                ShowRegisterMessage("Vui lòng nhập mật khẩu.", errorColor);
                return;
            }

            if (password.Length < 6)
            {
                ShowRegisterMessage("Mật khẩu phải có ít nhất 6 ký tự.", errorColor);
                return;
            }

            if (password.Length > 100)
            {
                ShowRegisterMessage("Mật khẩu không được dài quá 100 ký tự.", errorColor);
                return;
            }

            // 3. Validate Confirm Password
            if (string.IsNullOrEmpty(confirmPassword))
            {
                ShowRegisterMessage("Vui lòng nhập lại mật khẩu xác nhận.", errorColor);
                return;
            }

            if (password != confirmPassword)
            {
                ShowRegisterMessage("Mật khẩu xác nhận không trùng khớp.", errorColor);
                return;
            }

            // Bắt đầu gửi request đăng ký
            SetRegisterSubmitting(true);
            ShowRegisterMessage("Đang tạo tài khoản, vui lòng chờ...", Color.white);

            if (AuthManager.Instance == null || AuthManager.Instance.AuthService == null)
            {
                ShowRegisterMessage("Lỗi hệ thống: Không tìm thấy AuthManager.", errorColor);
                SetRegisterSubmitting(false);
                return;
            }

            AuthManager.Instance.AuthService.Register(username, password, OnRegisterSuccess, OnRegisterError);
        }

        private void OnRegisterSuccess(AuthResult result)
        {
            if (isDestroyed) return;

            Debug.Log($"[RegisterUI] Đăng ký thành công cho người dùng: {result.Username}");
            SetRegisterSubmitting(false);

            // Chuyển về LoginPanel và điền sẵn username
            OpenLoginPanel();

            if (emailInput != null)
                emailInput.text = result.Username;

            if (passwordInput != null)
            {
                passwordInput.text = string.Empty;
                passwordInput.Select();
            }

            ShowLoginMessage("Đăng ký tài khoản thành công! Vui lòng nhập mật khẩu để đăng nhập.", successColor);
        }

        private void OnRegisterError(string errorMessage)
        {
            if (isDestroyed) return;

            Debug.LogError($"[RegisterUI] Lỗi đăng ký: {errorMessage}");
            ShowRegisterMessage(errorMessage, errorColor);
            SetRegisterSubmitting(false);

            // Xóa trắng mật khẩu đã nhập để bảo mật
            if (regPasswordInput != null) regPasswordInput.text = string.Empty;
            if (regConfirmPasswordInput != null) regConfirmPasswordInput.text = string.Empty;
        }

        #endregion

        #region Helper Methods

        private void SetLoginSubmitting(bool value)
        {
            isSubmitting = value;

            if (loginButton != null) loginButton.interactable = !value;
            if (openRegisterButton != null) openRegisterButton.interactable = !value;
            if (emailInput != null) emailInput.interactable = !value;
            if (passwordInput != null) passwordInput.interactable = !value;

            if (loadingIndicator != null) loadingIndicator.SetActive(value);
        }

        private void SetRegisterSubmitting(bool value)
        {
            isSubmitting = value;

            if (submitRegisterButton != null) submitRegisterButton.interactable = !value;
            if (backToLoginButton != null) backToLoginButton.interactable = !value;
            if (regUsernameInput != null) regUsernameInput.interactable = !value;
            if (regPasswordInput != null) regPasswordInput.interactable = !value;
            if (regConfirmPasswordInput != null) regConfirmPasswordInput.interactable = !value;

            if (regLoadingIndicator != null) regLoadingIndicator.SetActive(value);
        }

        private void ShowLoginMessage(string message, Color color)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageText.color = color;
        }

        private void ShowRegisterMessage(string message, Color color)
        {
            if (regMessageText == null) return;
            regMessageText.text = message;
            regMessageText.color = color;
        }

        #endregion
    }
}
