using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;

namespace UrlShortener.Api.Tests;

public sealed class StartupTests
{
    [Fact]
    public void FactoriesKeepTheirSqliteDataIsolated()
    {
        using var first = new ApiFactory();
        using var second = new ApiFactory();
        first.Seed("OnlyOne");
        Assert.Equal(1, first.RowCount());
        Assert.Equal(0, second.RowCount());
        Assert.Null(second.ReadLink("OnlyOne"));
    }

    [Fact]
    public async Task DevelopmentStartupInitializesConfiguredDatabaseAndServesDocumentation()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".db");
        string? poolConnectionString = null;
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Links"] = $"Data Source={path}"
                }));
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
            using var opened = factory.Services.GetRequiredService<UrlShortener.Api.Shared.SqliteConnectionFactory>().OpenConnection();
            poolConnectionString = opened.ConnectionString;
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/docs/scalar", TestContext.Current.CancellationToken)).StatusCode);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                using var connection = new SqliteConnection(poolConnectionString ?? $"Data Source={path}");
                connection.Open();
                SqliteConnection.ClearPool(connection);
            }
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                File.Delete(path + suffix);
        }
    }
}
