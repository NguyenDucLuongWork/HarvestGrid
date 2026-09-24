using System;

namespace HarvestGrid.Managers.Auth
{
    public class AuthResult
    {
        public string PlayerId { get; set; }
        public string Username { get; set; }
        public string SessionToken { get; set; }
    }

    public interface IAuthService
    {
        void Login(string username, string password, Action<AuthResult> onSuccess, Action<string> onError);
        void Logout();
    }
}
