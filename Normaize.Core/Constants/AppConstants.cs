namespace Normaize.Core.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Configuration status constants
    /// </summary>
    public static class ConfigStatus
    {
        public const string SET = "SET";
        public const string NOT_SET = "NOT SET";
        public const string REDACTED = "[REDACTED]";
    }

    /// <summary>
    /// Environment constants
    /// </summary>
    public static class Environment
    {
        public const string DEVELOPMENT = "Development";
        public const string STAGING = "Staging";
        public const string PRODUCTION = "Production";
        public const string BETA = "Beta";
    }

    /// <summary>
    /// Storage provider constants
    /// </summary>
    public static class StorageProvider
    {
        public const string MEMORY = "memory";
        public const string S3 = "s3";
        public const string LOCAL = "local";
    }

    /// <summary>
    /// HTTP status messages
    /// </summary>
    public static class Messages
    {
        public const string SUCCESS = "Success";
        public const string ERROR = "Error";
        public const string NOT_FOUND = "Not Found";
        public const string UNAUTHORIZED = "Unauthorized";
    }

    /// <summary>
    /// Authentication and authorization constants
    /// </summary>
    public static class Auth
    {
        public const string BEARER = "Bearer";
        public const string AUTHORIZATION_HEADER = "Authorization";
        public const string JWT_SCHEME = "Bearer";
    }
} 