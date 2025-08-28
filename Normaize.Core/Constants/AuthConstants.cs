namespace Normaize.Core.Constants;

/// <summary>
/// Authentication and authorization related constants
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// Authentication and authorization constants
    /// </summary>
    public static class Auth
    {
        public const string BEARER = "Bearer";
        public const string AUTHORIZATION_HEADER = "Authorization";
        public const string JWT_SCHEME = "Bearer";
        public const string AnonymousUser = "anonymous";
    }

    /// <summary>
    /// CORS configuration constants
    /// </summary>
    public static class Cors
    {
        // Development origins
        public const string LOCALHOST_3000 = "http://localhost:3000";
        public const string LOCALHOST_4200 = "http://localhost:4200";
        public const string LOCALHOST_8080 = "http://localhost:8080";
        public const string LOCALHOST_5173 = "http://localhost:5173";  // Vite/React default
        public const string LOCALHOST_127_3000 = "http://127.0.0.1:3000";
        public const string LOCALHOST_127_4200 = "http://127.0.0.1:4200";
        public const string LOCALHOST_127_8080 = "http://127.0.0.1:8080";
        public const string LOCALHOST_127_5173 = "http://127.0.0.1:5173";  // Vite/React default

        // Production origins
        public const string NORMAIZE_COM = "https://normaize.com";
        public const string WWW_NORMAIZE_COM = "https://www.normaize.com";
        public const string APP_NORMAIZE_COM = "https://app.normaize.com";
        public const string BETA_NORMAIZE_COM = "https://beta.normaize.com";

        // HTTP methods
        public const string GET = "GET";
        public const string POST = "POST";
        public const string PUT = "PUT";
        public const string DELETE = "DELETE";
        public const string OPTIONS = "OPTIONS";

        // Headers
        public const string CONTENT_TYPE = "Content-Type";
        public const string AUTHORIZATION = "Authorization";
        public const string X_REQUESTED_WITH = "X-Requested-With";
        public const string ACCEPT = "Accept";

        // Policy names
        public const string DEVELOPMENT_POLICY = "Development";
        public const string BETA_POLICY = "Beta";
        public const string PRODUCTION_POLICY = "Production";
    }
}
