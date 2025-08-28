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
        public const string DEFAULT_PORT = "3306";
        public const string DEFAULT_HOST = "localhost";
        public const string DEFAULT_DATABASE = "testdb";
        public const string DEFAULT_USER = "testuser";
        public const string DEFAULT_PASSWORD = "testpass";
        public const string DEFAULT_CHARSET = "utf8mb4";

        // Connection string parts
        public const string SERVER_PREFIX = "Server=";
        public const string DATABASE_PREFIX = "Database=";
        public const string USER_PREFIX = "User=";
        public const string PASSWORD_PREFIX = "Password=";
        public const string PORT_PREFIX = "Port=";
        public const string CHARSET_PREFIX = "CharSet=";

        // MySQL specific constants
        public const string MYSQL_VERSION = "8.0.0";
        public const string ALLOW_LOAD_LOCAL_INFILE = "AllowLoadLocalInfile=true";
        public const string CONVERT_ZERO_DATETIME = "Convert Zero Datetime=True";
        public const string ALLOW_ZERO_DATETIME = "Allow Zero Datetime=True";
        public const string TEST_DATABASE_NAME = "TestDatabase";
    }
}
