
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabTestAccountCreator : MonoBehaviour
{
    [Header("Temporary test account")]
    [SerializeField] private string testUsername = "Admin";
    [SerializeField] private string testEmail = "genshin979695@gmail.com";
    [SerializeField] private string testPassword = "admin123";

    [ContextMenu("Register Test Account")]
    public void RegisterTestAccount()
    {
        var request = new RegisterPlayFabUserRequest
        {
            Username = testUsername,
            Email = testEmail,
            Password = testPassword,
            RequireBothUsernameAndEmail = true
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            result =>
            {
                Debug.Log("TEST ACCOUNT REGISTERED SUCCESSFULLY");
                Debug.Log("PlayFabId: " + result.PlayFabId);
                Debug.Log("Username: " + result.Username);
                Debug.Log("Bạn có thể dùng tài khoản này để test Login.");
            },
            error =>
            {
                Debug.LogError(
                    "TEST ACCOUNT REGISTRATION FAILED:\n"
                    + error.GenerateErrorReport()
                );
            }
        );
    }
}
