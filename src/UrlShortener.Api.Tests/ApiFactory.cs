using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool ownsDatabase;
    private string? poolConnectionString;
    private bool cleaned;
    public string DatabasePath { get; }
    public TimeProvider Clock { get; }
    public bool FailConnections { get; set; }
    public Action? ConnectionOpened { get; set; }

    public ApiFactory(string? databasePath = null, TimeProvider? clock = null)
    {
        ownsDatabase = databasePath is null;
        DatabasePath = databasePath ?? Path.Combine(Path.GetTempPath(), "url-shortener-" + Guid.NewGuid().ToString("N") + ".db");
        Clock = clock ?? new TestClock();
        ClientOptions.AllowAutoRedirect = false;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Links"] = $"Data Source={DatabasePath}",
            ["Logging:LogLevel:Default"] = "Error"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(Clock);
            services.RemoveAll<SqliteConnectionFactory>();
            services.AddSingleton<SqliteConnectionFactory>(provider => new FailingConnectionFactory(
                provider.GetRequiredService<IConfiguration>(), provider.GetRequiredService<IHostEnvironment>().ContentRootPath,
                () => FailConnections, value => { poolConnectionString = value; ConnectionOpened?.Invoke(); }));
            services.AddSingleton<IStartupFilter, TestAddressFilter>();
        });
    }

    public void Seed(string code, string destination = "https://example.com/destination", DateTimeOffset? expiresAt = null, long count = 0)
    {
        using var connection = Services.GetRequiredService<SqliteConnectionFactory>().OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO links(code, destination, expires_at_utc, access_count) VALUES ($code, $destination, $expires, $count)";
        command.Parameters.AddWithValue("$code", code);
        command.Parameters.AddWithValue("$destination", destination);
        command.Parameters.AddWithValue("$expires", (object?)expiresAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$count", count);
        command.ExecuteNonQuery();
    }

    public (string Destination, string? Expires, long Count)? ReadLink(string code)
    {
        using var connection = Services.GetRequiredService<SqliteConnectionFactory>().OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT destination, expires_at_utc, access_count FROM links WHERE code = $code COLLATE BINARY";
        command.Parameters.AddWithValue("$code", code);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.GetInt64(2)) : null;
    }

    public long RowCount()
    {
        using var connection = Services.GetRequiredService<SqliteConnectionFactory>().OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM links";
        return (long)command.ExecuteScalar()!;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || cleaned)
            return;
        cleaned = true;
        if (poolConnectionString is not null)
        {
            using var connection = new SqliteConnection(poolConnectionString);
            SqliteConnection.ClearPool(connection);
        }
        if (ownsDatabase)
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                File.Delete(DatabasePath + suffix);
    }

    private sealed class FailingConnectionFactory(IConfiguration configuration, string contentRoot, Func<bool> fail, Action<string> opened)
        : SqliteConnectionFactory(configuration, contentRoot)
    {
        public override SqliteConnection OpenConnection()
        {
            if (fail())
                throw new SqliteException("Ligação indisponível antes da operação.", 14);
            var connection = base.OpenConnection();
            opened(connection.ConnectionString);
            return connection;
        }
    }

    private sealed class TestAddressFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, continuation) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(context.Request.Headers["X-Test-IP"].FirstOrDefault() ?? "192.0.2.1");
                return continuation(context);
            });
            next(app);
        };
    }
}
