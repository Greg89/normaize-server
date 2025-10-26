using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.API.Tests.Infrastructure;

/// <summary>
/// Test application factory for integration tests with isolated database per test class
/// </summary>
public class ApiTestApplicationFactory : BaseTestApplicationFactory
{
    public async Task<HttpClient> CreateClientWithDataAsync()
    {
        await SeedTestDataAsync();
        return CreateClient();
    }
}