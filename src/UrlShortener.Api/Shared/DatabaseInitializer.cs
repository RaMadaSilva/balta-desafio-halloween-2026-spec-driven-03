namespace UrlShortener.Api.Shared;

public static class DatabaseInitializer
{
    public static void Initialize(SqliteConnectionFactory factory)
    {
        using var connection = factory.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL";
        if (!string.Equals(command.ExecuteScalar()?.ToString(), "wal", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Não foi possível ativar WAL na base SQLite.");

        using var transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA user_version";
        var version = Convert.ToInt64(command.ExecuteScalar());
        if (version > 1)
            throw new InvalidOperationException("A versão da base SQLite é superior à suportada.");

        if (version == 0)
        {
            command.CommandText = """
                CREATE TABLE links (
                    code TEXT PRIMARY KEY COLLATE BINARY
                        CHECK(length(code) BETWEEN 4 AND 20)
                        CHECK(code NOT GLOB '*[^0-9A-Za-z]*'),
                    destination TEXT NOT NULL CHECK(length(destination) BETWEEN 1 AND 2048),
                    expires_at_utc TEXT NULL,
                    access_count INTEGER NOT NULL DEFAULT 0 CHECK(access_count >= 0)
                ) STRICT, WITHOUT ROWID;
                PRAGMA user_version=1;
                """;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
