using Microsoft.Data.Sqlite;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void InitializerCreatesDurableConstrainedSchemaAndCanRunTwice()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Links"] = "Data Source=links.db"
            }).Build();
            var factory = new SqliteConnectionFactory(configuration, folder);
            DatabaseInitializer.Initialize(factory);
            DatabaseInitializer.Initialize(factory);
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version";
            Assert.Equal(1L, command.ExecuteScalar());
            command.CommandText = "PRAGMA journal_mode";
            Assert.Equal("wal", command.ExecuteScalar());
            command.CommandText = "INSERT INTO links(code, destination) VALUES ('Abcd', 'https://example.com')";
            Assert.Equal(1, command.ExecuteNonQuery());
            command.CommandText = "INSERT INTO links(code, destination) VALUES ('abcd', 'https://example.com')";
            Assert.Equal(1, command.ExecuteNonQuery());
            command.CommandText = "INSERT INTO links(code, destination) VALUES ('bad!', 'https://example.com')";
            Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
            command.CommandText = "UPDATE links SET access_count = -1";
            Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
            command.CommandText = "SELECT count(*) FROM links WHERE access_count = 0";
            Assert.Equal(2L, command.ExecuteScalar());
            SqliteConnection.ClearPool(connection);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void ConnectionFactoryRejectsMissingConfiguration()
    {
        var configuration = new ConfigurationBuilder().Build();
        Assert.Throws<InvalidOperationException>(() => new SqliteConnectionFactory(configuration, Path.GetTempPath()));
    }

    [Fact]
    public void ConnectionFactoryOpensIndependentConnectionsWithRequiredSettings()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Links"] = "Data Source=links.db"
            }).Build();
            var factory = new SqliteConnectionFactory(configuration, folder);
            using var first = factory.OpenConnection();
            using var second = factory.OpenConnection();
            Assert.NotSame(first, second);
            Assert.Equal(Path.Combine(folder, "links.db"), first.DataSource);
            Assert.Equal(5, first.DefaultTimeout);
            using var command = first.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys";
            Assert.Equal(1L, command.ExecuteScalar());
            SqliteConnection.ClearPool(first);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
