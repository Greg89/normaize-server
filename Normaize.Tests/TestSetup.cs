using System;

namespace Normaize.Tests
{
    public static class TestSetup
    {
        static TestSetup()
        {
            // Clear all MySQL and storage env vars before any tests run
            Environment.SetEnvironmentVariable("MYSQLHOST", null);
            Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
            Environment.SetEnvironmentVariable("MYSQLUSER", null);
            Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
            Environment.SetEnvironmentVariable("MYSQLPORT", null);

            // Clear storage provider configuration
            Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);
            Environment.SetEnvironmentVariable("SFTP_HOST", null);
            Environment.SetEnvironmentVariable("SFTP_USERNAME", null);
            Environment.SetEnvironmentVariable("SFTP_PASSWORD", null);
            Environment.SetEnvironmentVariable("SFTP_PRIVATE_KEY", null);
            Environment.SetEnvironmentVariable("SFTP_PRIVATE_KEY_PATH", null);
            Environment.SetEnvironmentVariable("SFTP_BASEPATH", null);

            // Clear other environment variables that might interfere
            Environment.SetEnvironmentVariable("SEQ_URL", null);
            Environment.SetEnvironmentVariable("SEQ_API_KEY", null);
            Environment.SetEnvironmentVariable("AUTH0_ISSUER", null);
            Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", null);

            // Force Test environment
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

            // Ensure the environment is set for the current process
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Test");
        }
    }
}