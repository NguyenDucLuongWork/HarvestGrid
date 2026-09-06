namespace HarvestGrid.UI
{
    public static class AuthSession
    {
        public static string UserId { get; private set; }
        public static string Email { get; private set; }
        public static string IdToken { get; private set; }

        public static bool IsAuthenticated =>
            !string.IsNullOrEmpty(UserId) &&
            !string.IsNullOrEmpty(IdToken);

        public static void Set(string userId, string email, string idToken)
        {
            UserId = userId;
            Email = email;
            IdToken = idToken;
        }

        public static void Clear()
        {
            UserId = null;
            Email = null;
            IdToken = null;
        }
    }
}
