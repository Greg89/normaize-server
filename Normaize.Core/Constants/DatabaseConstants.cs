namespace Normaize.Core.Constants;

/// <summary>
/// Database configuration and operation constants
/// </summary>
public static class DatabaseConstants
{
    /// <summary>
    /// Database configuration constants
    /// </summary>
    public static class Database
    {
        // Default database configuration values
        public const string DEFAULT_PORT = "5432";
        public const string DEFAULT_HOST = "localhost";
        public const string DEFAULT_DATABASE = "normaize";
        public const string DEFAULT_USER = "normaize_user";
        public const string DEFAULT_PASSWORD = "normaize_password";

        // Connection string parts
        public const string SERVER_PREFIX = "Host=";
        public const string DATABASE_PREFIX = "Database=";
        public const string USER_PREFIX = "Username=";
        public const string PASSWORD_PREFIX = "Password=";
        public const string PORT_PREFIX = "Port=";
        public const string TEST_DATABASE_NAME = "TestDatabase";
    }
}
