
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

                    onError?.Invoke(
                        "Đăng nhập thất bại. " +
                        "Vui lòng kiểm tra tài khoản, " +
                        "mật khẩu hoặc kết nối mạng.");
                });
        }

        public void Logout()
        {
            // AuthManager sẽ xử lý việc xóa
            // session local khi đăng xuất.
        }
    }
}
