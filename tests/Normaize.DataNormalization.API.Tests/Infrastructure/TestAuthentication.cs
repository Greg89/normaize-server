namespace Normaize.DataNormalization.API.Tests.Infrastructure;

/// <summary>
/// Shared auth helper for integration tests.
/// Keeps authentication setup consistent across test classes.
/// </summary>
public static class TestAuthentication
{
    public static HttpClient CreateAuthenticatedClient(
        this ApiTestApplicationFactory factory,
        string userId = "test-user-id",
        string email = "test@example.com",
        string name = "Test User")
    {
        // BaseTestApplicationFactory already registers the "Test" scheme as default.
        // Here we only set the headers used by TestAuthenticationHandler.
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        client.DefaultRequestHeaders.Add("X-Test-User-Email", email);
        client.DefaultRequestHeaders.Add("X-Test-User-Name", name);

        return client;
    }
}
