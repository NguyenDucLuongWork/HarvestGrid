namespace HarvestGrid.UI
{
    public static class AuthSession
    {
        public static string UserId { get; private set; }
        public static string Username { get; private set; }
        public static string SessionToken { get; private set; }

        public static bool IsAuthenticated =>
            !string.IsNullOrEmpty(UserId) &&
            !string.IsNullOrEmpty(SessionToken);

        public static void Set(string userId, string username, string sessionToken)
        {
            UserId = userId;
            Username = username;
            SessionToken = sessionToken;
        }

        public static void Clear()
        {
            UserId = null;
            Username = null;
            SessionToken = null;
        }
    }
}
