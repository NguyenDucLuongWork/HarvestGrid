
using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using HarvestGrid.UI;
using LgTyLib.Modules.DataPersistence;

namespace HarvestGrid.Managers.Auth
{
    public class PlayFabAuthService : IAuthService
    {
        private const string PLAYFAB_TITLE_ID = "14B566";

        public PlayFabAuthService()
        {
            if (string.IsNullOrEmpty(
                PlayFabSettings.staticSettings.TitleId))
            {
                PlayFabSettings.staticSettings.TitleId =
                    PLAYFAB_TITLE_ID;
            }
        }

        public void Login(
            string username,
            string password,
            Action<AuthResult> onSuccess,
            Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                onError?.Invoke(
                    "Tên đăng nhập không được để trống.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                onError?.Invoke(
                    "Mật khẩu không được để trống.");
                return;
            }

            var request = new LoginWithPlayFabRequest
            {
                Username = username,
                Password = password,
                InfoRequestParameters =
                    new GetPlayerCombinedInfoRequestParams
                    {
                        GetUserAccountInfo = true
                    }
            };

            Debug.Log("[PlayFab] Bắt đầu gửi yêu cầu Login...");
            PlayFabClientAPI.LoginWithPlayFab(
                request,
                result =>
                {
                    Debug.Log("[PlayFab] LOGIN SUCCESS");
                    Debug.Log("[PlayFab] PlayFabId: " + result.PlayFabId);
                    AuthSession.Set(
                    result.PlayFabId,
                    username,
                    result.SessionTicket
                    );

                    DataPersistenceManager.Instance.SetCurrentPlayer(
                        result.PlayFabId,
                        username
                    );

                    DataPersistenceManager.Instance.LoadCloudSaves(success =>
                    {
                        Debug.Log(
                            "[CloudSave] Callback được gọi. Success = " + success
                        );
                    if (!success)
                        {
                            Debug.LogError(
                                "[CloudSave] Không tải được save từ cloud."
                            );

                            onError?.Invoke(
                                "Đăng nhập thành công nhưng không thể tải dữ liệu lưu. " +
                                "Vui lòng thử lại."
                            );

                            return;
                        }
                            Debug.Log("[CloudSave] Tải save thành công.");
                            onSuccess?.Invoke(new AuthResult
                        {
                            PlayerId = result.PlayFabId,
                            Username = username,
                            SessionToken = result.SessionTicket
                        });
                    
                    });
                },
                error =>
                {
                    Debug.LogError("[PlayFab] LOGIN FAILED");
                    Debug.LogError(
                        error.GenerateErrorReport());

                    string friendlyError = TranslatePlayFabError(error);
                    onError?.Invoke(friendlyError);
                });
        }

        public void Register(
            string username,
            string password,
            Action<AuthResult> onSuccess,
            Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                onError?.Invoke("Tên đăng nhập không được để trống.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                onError?.Invoke("Mật khẩu không được để trống.");
                return;
            }

            var request = new RegisterPlayFabUserRequest
            {
                Username = username,
                Password = password,
                DisplayName = username,
                RequireBothUsernameAndEmail = false
            };

            Debug.Log("[PlayFab] Bắt đầu gửi yêu cầu Register...");
            PlayFabClientAPI.RegisterPlayFabUser(
                request,
                result =>
                {
                    Debug.Log("[PlayFab] REGISTER SUCCESS");
                    Debug.Log("[PlayFab] PlayFabId: " + result.PlayFabId);

                    onSuccess?.Invoke(new AuthResult
                    {
                        PlayerId = result.PlayFabId,
                        Username = result.Username ?? username,
                        SessionToken = result.SessionTicket
                    });
                },
                error =>
                {
                    Debug.LogError("[PlayFab] REGISTER FAILED");
                    Debug.LogError(error.GenerateErrorReport());

                    string friendlyError = TranslatePlayFabError(error);
                    onError?.Invoke(friendlyError);
                });
        }

        private string TranslatePlayFabError(PlayFabError error)
        {
            if (error == null) return "Đã xảy ra lỗi không xác định.";

            if (error.ErrorDetails != null && error.ErrorDetails.Count > 0)
            {
                foreach (var kvp in error.ErrorDetails)
                {
                    if (kvp.Value != null && kvp.Value.Count > 0)
                    {
                        string detail = string.Join("; ", kvp.Value);
                        if (detail.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                            return "Mật khẩu phải từ 6 đến 100 ký tự.";
                        if (detail.IndexOf("Username", StringComparison.OrdinalIgnoreCase) >= 0)
                            return "Tên đăng nhập phải từ 3 đến 20 ký tự và không chứa ký tự đặc biệt.";
                    }
                }
            }

            return error.Error switch
            {
                PlayFabErrorCode.UsernameNotAvailable => "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.",
                PlayFabErrorCode.InvalidUsername => "Tên đăng nhập không hợp lệ (từ 3-20 ký tự, chỉ chứa chữ cái, số và dấu gạch dưới).",
                PlayFabErrorCode.InvalidPassword => "Mật khẩu không hợp lệ (phải có từ 6 ký tự trở lên).",
                PlayFabErrorCode.InvalidUsernameOrPassword => "Tên đăng nhập hoặc mật khẩu không đúng.",
                PlayFabErrorCode.AccountNotFound => "Tài khoản không tồn tại. Vui lòng kiểm tra lại hoặc đăng ký mới.",
                PlayFabErrorCode.ConnectionError => "Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.",
                PlayFabErrorCode.ServiceUnavailable => "Máy chủ PlayFab tạm thời không khả dụng. Vui lòng thử lại sau.",
                PlayFabErrorCode.APIClientRequestRateLimitExceeded or 
                PlayFabErrorCode.APIRequestLimitExceeded or 
                PlayFabErrorCode.OverLimit => "Bạn đang gửi yêu cầu quá nhanh. Vui lòng chờ vài giây rồi thử lại.",
                _ => string.IsNullOrEmpty(error.ErrorMessage)
                    ? "Đã xảy ra lỗi khi kết nối PlayFab. Vui lòng thử lại."
                    : $"Lỗi: {error.ErrorMessage}"
            };
        }

        public void Logout()
        {
            // AuthManager sẽ xử lý việc xóa
            // session local khi đăng xuất.
        }
    }
}
