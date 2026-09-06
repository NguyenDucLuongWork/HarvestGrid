using System;
using Firebase;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HarvestGrid.UI
{
    public class LoginUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private string nextSceneName = "SampleScene";

        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI messageText;

        private FirebaseAuth auth;
        private bool firebaseReady;
        private bool isSubmitting;
        private bool hasRuntimeLoginListener;
        private bool isDestroyed;

        private async void Awake()
        {
            if (messageText == null) CreateMessageText();

            if (loginButton.onClick.GetPersistentEventCount() == 0)
            {
                loginButton.onClick.AddListener(Login);
                hasRuntimeLoginListener = true;
            }

            passwordInput.onSubmit.AddListener(OnPasswordSubmitted);
            SetSubmitting(true);
            ShowMessage("Đang khởi tạo Firebase...", new Color(1f, 0.75f, 0.2f));

            try
            {
                DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (isDestroyed) return;

                if (status != DependencyStatus.Available)
                {
                    ShowMessage($"Không thể khởi tạo Firebase: {status}.", Color.red);
                    return;
                }

                auth = FirebaseAuth.DefaultInstance;
                firebaseReady = true;
                ShowMessage(string.Empty, Color.white);
                SetSubmitting(false);
            }
            catch (Exception exception)
            {
                if (isDestroyed) return;
                Debug.LogException(exception);
                ShowMessage("Không thể khởi tạo Firebase. Vui lòng thử lại.", Color.red);
            }
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

        public async void Login()
        {
            if (isSubmitting) return;

            if (!firebaseReady || auth == null)
            {
                ShowMessage("Firebase chưa sẵn sàng. Vui lòng thử lại.", Color.red);
                return;
            }

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
            ShowMessage("Đang đăng nhập...", new Color(1f, 0.75f, 0.2f));

            try
            {
                AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
                if (isDestroyed) return;

                FirebaseUser user = result.User;
                if (user == null)
                    throw new InvalidOperationException("Firebase did not return a user.");

                string idToken = await user.TokenAsync(false);
                if (isDestroyed) return;

                AuthSession.Set(user.UserId, user.Email, idToken);
                ShowMessage("Đăng nhập thành công!", new Color(0.2f, 0.8f, 0.3f));

                if (!string.IsNullOrWhiteSpace(nextSceneName))
                    SceneManager.LoadScene(nextSceneName);
            }
            catch (Exception exception)
            {
                if (isDestroyed) return;

                Debug.LogException(exception);
                ShowMessage(GetErrorMessage(exception), Color.red);
                SetSubmitting(false);
            }
        }

        private static string GetErrorMessage(Exception exception)
        {
            FirebaseException firebaseException = FindFirebaseException(exception);
            if (firebaseException == null)
                return "Đăng nhập thất bại. Vui lòng thử lại.";

            switch ((AuthError)firebaseException.ErrorCode)
            {
                case AuthError.InvalidEmail:
                    return "Email không hợp lệ.";
                case AuthError.WrongPassword:
                case AuthError.UserNotFound:
                case AuthError.InvalidCredential:
                    return "Email hoặc mật khẩu không đúng.";
                case AuthError.UserDisabled:
                    return "Tài khoản này đã bị vô hiệu hóa.";
                case AuthError.TooManyRequests:
                    return "Quá nhiều lần đăng nhập. Vui lòng thử lại sau.";
                case AuthError.NetworkRequestFailed:
                    return "Không thể kết nối Firebase. Hãy kiểm tra mạng.";
                default:
                    return "Đăng nhập thất bại. Vui lòng thử lại.";
            }
        }

        private static FirebaseException FindFirebaseException(Exception exception)
        {
            if (exception is FirebaseException firebaseException)
                return firebaseException;

            if (exception is AggregateException aggregateException)
            {
                foreach (Exception innerException in aggregateException.Flatten().InnerExceptions)
                {
                    FirebaseException match = FindFirebaseException(innerException);
                    if (match != null) return match;
                }
            }

            return exception.InnerException == null
                ? null
                : FindFirebaseException(exception.InnerException);
        }

        private void SetSubmitting(bool value)
        {
            isSubmitting = value;
            loginButton.interactable = !value;
            emailInput.interactable = !value;
            passwordInput.interactable = !value;
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
